```markdown
# GPT API Client

Полнофункциональный асинхронный API-клиент для работы с OpenAI-совместимыми GPT API без использования официального SDK.

## Характеристики

✅ **Генерация текста** — создание текста по заданному промпту с системными сообщениями
✅ **Трансформация текста** — изменение существующего текста по инструкции
✅ **Потоковая генерация (Streaming)** — получение ответа в реальном времени чанками
✅ **Продолжение диалога** — работа с историей сообщений
✅ **Генерация изображений** — создание изображений по текстовому описанию
✅ **Анализ изображений (Vision)** — описание содержимого изображений
✅ **Редактирование изображений** — изменение изображений на основе анализа и промпта
✅ **Трансформация текста с контекстом изображения** — улучшение текста на основе визуального контекста
✅ **CancellationToken поддержка** — отмена операций в любое время
✅ **Конфигурация через IOptions<T>** — гибкая настройка через DI контейнер
✅ **Регистрация в DI** — легкая интеграция в ASP.NET Core приложения
✅ **Все публичные методы асинхронные** — полная поддержка async/await
✅ **Логирование** — встроенная поддержка структурированного логирования через ILogger

## Требования

- .NET 10.0+
- OpenAI 2.0.0
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.7
- Microsoft.Extensions.Http 10.0.7
- Microsoft.Extensions.Configuration.Abstractions 10.0.7
- Microsoft.Extensions.Logging.Abstractions 10.0.7
- Microsoft.Extensions.Options 10.0.7

## Установка

```bash
dotnet add package MyBestGptClient
```

## Быстрый старт

### 1. Регистрация в DI контейнере

```csharp
using GptClient.Extensions;

var services = new ServiceCollection();
services.AddLogging(config => config.AddConsole());

// Вариант 1: Конфигурация через лямбду
services.AddGptClient(options =>
{
    options.ApiKey = "sk-your-api-key-here";
    options.DefaultModel = "gpt-4";
    options.HttpTimeoutSeconds = 30;
    options.MaxRetryAttempts = 3;
    options.RequestsPerSecond = 100;
});

// Вариант 2: Конфигурация из IConfiguration
// services.AddGptClient(configuration, "GptClient");

var provider = services.BuildServiceProvider();
var gptService = provider.GetRequiredService<IGptService>();
```

### 2. Генерация текста

```csharp
var response = await gptService.GenerateTextAsync(
    prompt: "Привет! Расскажи о .NET.",
    systemMessage: "Ты полезный ассистент-программист.");

Console.WriteLine(response);
```

### 3. Потоковая генерация (Streaming)

```csharp
Console.WriteLine("Ответ ассистента:");
await foreach (var chunk in gptService.GenerateTextStreamAsync(
    prompt: "Напиши стихотворение про программиста"))
{
    Console.Write(chunk);
}
Console.WriteLine();
```

### 4. Трансформация текста

```csharp
var originalText = "Кот сидел на окне. За окном шёл дождь.";

var transformed = await gptService.TransformTextAsync(
    text: originalText,
    instruction: "Сделай текст более художественным и атмосферным");

Console.WriteLine(transformed);
```

### 5. Работа с диалогом

```csharp
using OpenAI.Chat;

var messages = new List<ChatMessage>
{
    new SystemChatMessage("Ты полезный ассистент."),
    new UserChatMessage("Расскажи о C#."),
    new AssistantChatMessage("C# — это современный объектно-ориентированный язык..."),
    new UserChatMessage("А чем он отличается от Java?")
};

var response = await gptService.ContinueDialogueAsync(messages);
Console.WriteLine(response);
```

### 6. Генерация изображений

```csharp
var imageUrl = await gptService.GenerateImageAsync(
    prompt: "Космический кот, играющий на гитаре на фоне галактики, цифровой арт");

Console.WriteLine($"Изображение сгенерировано: {imageUrl}");
```

### 7. Анализ изображений (Vision)

```csharp
var imageBytes = await File.ReadAllBytesAsync("photo.jpg");

var description = await gptService.DescribeImageAsync(
    imageBytes: imageBytes,
    analysisPrompt: "Опиши, что изображено на фото, какое настроение оно передаёт");

Console.WriteLine(description);
```

### 8. Редактирование изображений

```csharp
var imageBytes = await File.ReadAllBytesAsync("cat.jpg");

var editedImageUrl = await gptService.EditImageAsync(
    imageBytes: imageBytes,
    editPrompt: "Сделай кота рыжим и добавь ему солнечные очки");

Console.WriteLine($"Отредактированное изображение: {editedImageUrl}");
```

### 9. Улучшение текста на основе изображения

```csharp
var imageBytes = await File.ReadAllBytesAsync("product.jpg");
var productDescription = "Товар для повседневного использования.";

var enhancedDescription = await gptService.TransformTextWithImageContextAsync(
    text: productDescription,
    imageBytes: imageBytes,
    instruction: "Улучши описание товара, основываясь на том, что видно на изображении");

Console.WriteLine(enhancedDescription);
```

## Опции конфигурации

```csharp
public sealed class GptClientOptions
{
    /// <summary>
    /// API ключ для OpenAI-совместимого API
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Модель по умолчанию (например, gpt-4, gpt-3.5-turbo)
    /// </summary>
    public required string DefaultModel { get; set; }
    
    /// <summary>
    /// Модель по умолчанию для изображений (например, gpt-4, gpt-3.5-turbo)
    /// </summary>
    public required string DefaultImageModel { get; set; }

    /// <summary>
    /// Таймаут для HTTP запросов в секундах (по умолчанию: 30)
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Максимальное количество попыток переподключения (по умолчанию: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Начальная задержка перед первым ретраем в миллисекундах (по умолчанию: 100)
    /// </summary>
    public int InitialRetryDelayMs { get; set; } = 100;

    /// <summary>
    /// Максимальная задержка между ретраями в миллисекундах (по умолчанию: 10000)
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 10_000;

    /// <summary>
    /// Множитель для exponential backoff (по умолчанию: 2.0)
    /// </summary>
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Максимальное количество запросов в секунду для rate limiting (по умолчанию: 100)
    /// </summary>
    public int RequestsPerSecond { get; set; } = 100;

    /// <summary>
    /// Статус коды, при которых следует выполнить ретрай
    /// (по умолчанию: 408, 429, 500, 502, 503, 504)
    /// </summary>
    public int[] RetryableStatusCodes { get; set; } = { 408, 429, 500, 502, 503, 504 };
}
```

## Обработка ошибок

Клиент автоматически обрабатывает следующие типы ошибок:

- **UnauthorizedGptException** — ошибка авторизации (401)
- **TemporarilyException** — временная ошибка сервера (5xx, сетевые ошибки)
- **OperationCanceledException** — операция отменена через CancellationToken

```csharp
try
{
    var response = await gptService.GenerateTextAsync("Привет");
    Console.WriteLine(response);
}
catch (UnauthorizedGptException ex)
{
    Console.WriteLine($"Ошибка авторизации: {ex.Message}");
}
catch (TemporarilyException ex)
{
    Console.WriteLine($"Сервер временно недоступен: {ex.Message}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Операция отменена");
}
```

## Отмена операций

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var response = await gptService.GenerateTextAsync(
        prompt: "Напиши длинную историю",
        cancellationToken: cts.Token);
    
    Console.WriteLine(response);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Операция отменена по таймауту");
}
```

## Архитектура

### Интерфейсы

| Интерфейс | Назначение |
|-----------|------------|
| `IChatClient` | Низкоуровневый клиент для текстовых запросов и streaming |
| `IVisionClient` | Клиент для анализа изображений |
| `IImageClient` | Клиент для генерации изображений |
| `IOpenAiClient` | Объединённый клиент для всех OpenAI API |
| `IGptService` | Высокоуровневый сервис с удобным API |

### Основные компоненты

- **GptService** — основная реализация высокоуровневого сервиса
- **ChatClient** — реализация клиента для Chat Completions API
- **VisionClient** — реализация клиента для Vision API
- **ImageClient** — реализация клиента для Image Generation API
- **GptClientOptions** — конфигурация клиента

### Поток обработки запроса

```
GenerateTextAsync()
    ↓
Построение сообщений
    ↓
ChatClient.CompleteAsync()
    ↓
HTTP запрос к API
    ↓
Обработка ответа
    ↓
Возврат результата
```

## Структурированное логирование

Все операции логируются с контекстными данными:

- `Text generation: started` / `completed` / `cancelled` / `failed`
- `Text transformation: started` / `completed` / `cancelled` / `failed`
- `Image generation: started` / `completed` / `cancelled` / `failed`
- `Image analysis: started` / `completed` / `cancelled` / `failed`

Каждое сообщение содержит дополнительные поля (промпт, длина ответа, размер изображения и т.д.).

## Примеры кода

Полные примеры находятся в файле `Examples/UsageExamples.cs`

## Лицензия

MIT

## Поддержка

Для вопросов и проблем создавайте issue в репозитории проекта.
```
