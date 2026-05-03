# Руководство по внедрению GPT API Client

## Быстрый старт (5 минут)

### Шаг 1: Подготовка проекта

```bash
# Добавить NuGet пакеты
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.7
dotnet add package Microsoft.Extensions.Http --version 10.0.7
dotnet add package Microsoft.Extensions.Configuration.Abstractions --version 10.0.7
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 10.0.7
dotnet add package Microsoft.Extensions.Options --version 10.0.7
```

### Шаг 2: Регистрация в Program.cs

```csharp
using GptClient;
using Microsoft.Extensions.DependencyInjection;

// Вариант 1: С явной конфигурацией
services.AddGptClient(options =>
{
    options.ApiKey = "sk-your-api-key";
    options.BaseUrl = "https://api.openai.com/v1";
    options.DefaultModel = "gpt-4";
});

// Вариант 2: Из конфигурации
services.AddGptClient(configuration, "GptClient");
```

### Шаг 3: appsettings.json (если используется вариант 2)

```json
{
  "GptClient": {
    "ApiKey": "sk-your-api-key",
    "BaseUrl": "https://api.openai.com/v1",
    "DefaultModel": "gpt-4",
    "HttpTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RequestsPerSecond": 100
  }
}
```

### Шаг 4: Использование в коде

```csharp
using GptClient.Models;
using GptClient.Services;

public class MyService
{
    private readonly IGptService _gptService;

    public MyService(IGptService gptService)
    {
        _gptService = gptService;
    }

    public async Task<string> GetAnswerAsync(string question)
    {
        var messages = new List<ChatMessage>
        {
            new() { Role = "user", Content = question }
        };

        var response = await _gptService.SendMessageAsync(messages);
        return response.Choices[0].Message.Content;
    }
}
```

## Полная конфигурация

### Базовые опции

```csharp
services.AddGptClient(options =>
{
    // ТРЕБУЕМЫЕ ПАРАМЕТРЫ
    options.ApiKey = "sk-...";                    // OpenAI API ключ
    options.BaseUrl = "https://api.openai.com/v1"; // API URL
    options.DefaultModel = "gpt-4";               // Модель по умолчанию

    // ОПЦИОНАЛЬНЫЕ ПАРАМЕТРЫ
    options.HttpTimeoutSeconds = 30;              // Таймаут (сек)
    options.MaxRetryAttempts = 3;                 // Max попыток ретрая
    options.InitialRetryDelayMs = 100;            // Начальная задержка (ms)
    options.MaxRetryDelayMs = 10_000;             // Макс задержка (ms)
    options.RetryBackoffMultiplier = 2.0;        // Множитель backoff
    options.RequestsPerSecond = 100;              // Rate limit
    options.RetryableStatusCodes = new[] { 429, 500, 502, 503, 504 };
});
```

### Логирование

```csharp
// Program.cs
builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
    configure.SetMinimumLevel(LogLevel.Debug);
});

// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "GptClient": "Debug",
      "GptClient.Client.Impl": "Debug",
      "GptClient.Services": "Information"
    }
  }
}
```

## Сценарии использования

### 1. Простой чат

```csharp
var messages = new List<ChatMessage>
{
    new() { Role = "system", Content = "Ты помощник." },
    new() { Role = "user", Content = "Привет!" }
};

var response = await gptService.SendMessageAsync(messages);
Console.WriteLine(response.Choices[0].Message.Content);
```

### 2. Потоковый ответ

```csharp
Console.Write("Ответ: ");
await foreach (var chunk in gptService.SendMessageStreamAsync(messages))
{
    if (chunk.Choices[0].Delta?.Content is { Length: > 0 } content)
    {
        Console.Write(content);
    }
}
Console.WriteLine();
```

### 3. С отменой операции

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

try
{
    var response = await gptService.SendMessageAsync(
        messages,
        cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Операция отменена");
}
```

### 4. С контролем генерации

```csharp
var response = await gptService.SendMessageAsync(
    messages,
    model: "gpt-4",           // Переопределить модель
    temperature: 0.3,         // Детерминированно (точно)
    maxTokens: 100            // Ограничить длину ответа
);
```

### 5. ASP.NET Core контроллер

```csharp
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IGptService _gptService;

    public ChatController(IGptService gptService) => _gptService = gptService;

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] string message)
    {
        var response = await _gptService.SendMessageAsync(
            new[] { new ChatMessage { Role = "user", Content = message } });
        return Ok(new { answer = response.Choices[0].Message.Content });
    }

    [HttpGet("stream")]
    public async IAsyncEnumerable<string> StreamMessage([FromQuery] string question)
    {
        await foreach (var chunk in _gptService.SendMessageStreamAsync(
            new[] { new ChatMessage { Role = "user", Content = question } }))
        {
            yield return chunk.Choices[0].Delta?.Content ?? "";
        }
    }
}
```

### 6. Обработка ошибок

```csharp
using GptClient.Exceptions;

try
{
    var response = await gptService.SendMessageAsync(messages);
}
catch (UnauthorizedGptException ex)
{
    // API ключ неверный
    logger.LogError(ex, "Ошибка авторизации");
}
catch (TemporarilyException ex)
{
    // Сервер временно недоступен (уже было 3 ретрая)
    logger.LogError(ex, "Сервер недоступен после ретраев");
}
catch (OperationCanceledException)
{
    // Пользователь отменил операцию
    logger.LogInformation("Операция отменена пользователем");
}
catch (Exception ex)
{
    // Другие ошибки
    logger.LogError(ex, "Неожиданная ошибка");
}
```

## Оптимизация производительности

### 1. Повторное использование сервиса

❌ **Неправильно:**
```csharp
for (int i = 0; i < 100; i++)
{
    var factory = serviceProvider.GetRequiredService<IGptServiceFactory>();
    var service = factory.Create(); // Создание нового каждый раз!
}
```

✅ **Правильно:**
```csharp
var service = serviceProvider.GetRequiredService<IGptService>(); // Singleton
for (int i = 0; i < 100; i++)
{
    var response = await service.SendMessageAsync(messages);
}
```

### 2. Streaming для больших ответов

❌ **Неправильно:**
```csharp
// Весь ответ загружается в памяти
var response = await service.SendMessageAsync(messages, maxTokens: 5000);
```

✅ **Правильно:**
```csharp
// Ответ обрабатывается по частям
await foreach (var chunk in service.SendMessageStreamAsync(messages))
{
    // Обработать chunk и отправить клиенту
}
```

### 3. Контроль Rate Limiting

```csharp
// Если нужно более ограниченное rate limiting
services.AddGptClient(options =>
{
    options.RequestsPerSecond = 10; // Более строгий лимит
});

// Система автоматически будет ждать между запросами
// если превышен лимит
```

### 4. Оптимизация timeout

```csharp
services.AddGptClient(options =>
{
    // Для коротких запросов
    options.HttpTimeoutSeconds = 10;
    
    // Для длинных операций (streaming)
    options.HttpTimeoutSeconds = 120;
});
```

## Мониторинг и диагностика

### Логирование

```csharp
// Program.cs
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});

// Будут видны все операции:
// - Попытки и ретраи
// - Rate limiting
// - Ошибки парсинга
// - HTTP запросы/ответы
```

### Проверка метрик

```csharp
var response = await gptService.SendMessageAsync(messages);

if (response.Usage != null)
{
    Console.WriteLine($"Prompt tokens: {response.Usage.PromptTokens}");
    Console.WriteLine($"Completion tokens: {response.Usage.CompletionTokens}");
    Console.WriteLine($"Total tokens: {response.Usage.TotalTokens}");
}
```

### Отладка SSL ошибок

```csharp
// Если проблемы с SSL сертификатом (ТОЛЬКО ДЛЯ РАЗРАБОТКИ!)
HttpClientHandler handler = new();
handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
services.AddHttpClient()
    .ConfigureHttpClient(c => { });
```

## Миграция с других SDK

### С OpenAI SDK

```csharp
// Было:
var response = await client.ChatCompletions.CreateChatCompletionAsync(...);

// Стало:
var messages = new List<ChatMessage> { ... };
var response = await gptService.SendMessageAsync(messages);
```

### Конвертация сообщений

```csharp
// OpenAI format → GptClient format
var gptMessages = openaiMessages
    .Select(m => new ChatMessage 
    { 
        Role = m.Role.ToString().ToLower(),
        Content = m.Content
    })
    .ToList();

var response = await gptService.SendMessageAsync(gptMessages);
```

## Тестирование

### Unit тесты

```csharp
[Test]
public async Task SendMessage_WithValidMessage_ReturnsResponse()
{
    // Arrange
    var mockService = new Mock<IGptService>();
    mockService
        .Setup(s => s.SendMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), 
            null, null, null, default))
        .ReturnsAsync(new ChatCompletionResponse 
        { 
            Choices = new[] { new ChatCompletionChoice 
            { 
                Message = new ChatMessage { Role = "assistant", Content = "Test" } 
            }}
        });

    // Act
    var result = await mockService.Object.SendMessageAsync(messages);

    // Assert
    Assert.That(result.Choices[0].Message.Content, Is.EqualTo("Test"));
}
```

### Integration тесты

```csharp
[Test]
[Explicit("Requires valid API key")]
public async Task RealApiIntegration()
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddGptClient(options =>
    {
        options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        options.BaseUrl = "https://api.openai.com/v1";
        options.DefaultModel = "gpt-3.5-turbo";
    });

    var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredService<IGptService>();

    var response = await service.SendMessageAsync(new[]
    {
        new ChatMessage { Role = "user", Content = "Say 'hello'" }
    });

    Assert.That(response.Choices.Length, Is.GreaterThan(0));
}
```

## Часто задаваемые вопросы

### В: Как использовать собственный OpenAI-совместимый сервис?

О: Просто измените `BaseUrl`:
```csharp
options.BaseUrl = "https://my-custom-api.com/v1";
```

### В: Как отключить ретраи?

О: Установите `MaxRetryAttempts = 1` (1 попытка = нет ретраев)

### В: Как увеличить таймаут для длинных операций?

О: Измените `HttpTimeoutSeconds`:
```csharp
options.HttpTimeoutSeconds = 300; // 5 минут
```

### В: Как обработать rate limiting ошибку (429)?

О: Код `429` по умолчанию в `RetryableStatusCodes`, поэтому будет автоматический ретрай

### В: Можно ли использовать несколько моделей?

О: Да, переопределите модель в каждом вызове:
```csharp
var response = await service.SendMessageAsync(messages, model: "gpt-3.5-turbo");
```

### В: Как уменьшить использование памяти?

О: Используйте streaming для больших ответов:
```csharp
await foreach (var chunk in service.SendMessageStreamAsync(messages))
{
    // Обработать сразу, не сохранять в памяти
}
```

## Производственные рекомендации

1. **Храните API ключ в secrets** (не в коде!)
   ```csharp
   options.ApiKey = configuration["GptClient:ApiKey"];
   ```

2. **Используйте логирование** для отладки проблем

3. **Мониторьте использование токенов** для контроля затрат

4. **Установите reasonable timeout** чтобы не повесить приложение

5. **Обрабатывайте ошибки** специфично для разных типов

6. **Тестируйте** перед использованием в production

7. **Используйте streaming** для больших ответов

## Поддержка и контакты

Для вопросов и проблем обращайтесь в документацию или создавайте issue в репозитории.
