# GPT API Client

Полнофункциональный асинхронный API-клиент для работы с OpenAI-совместимыми GPT API без использования официального SDK.

## Характеристики

✅ **Поддержка Streaming и Non-Streaming запросов** - выбирайте нужный формат ответа
✅ **Exponential Backoff Retry** - автоматическое восстановление при временных ошибках
✅ **Rate Limiting** - управление лимитом запросов в секунду (используется `System.Threading.RateLimiting`)
✅ **CancellationToken поддержка** - отмена операций в любое время
✅ **Конфигурация через IOptions<T>** - гибкая настройка через DI контейнер
✅ **Регистрация в DI** - легкая интеграция в ASP.NET Core приложения
✅ **Все публичные методы асинхронные** - полная поддержка async/await
✅ **Логирование** - встроенная поддержка логирования через ILogger

## Требования

- .NET 10.0+
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.7
- Microsoft.Extensions.Http 10.0.7
- Microsoft.Extensions.Configuration.Abstractions 10.0.7
- Microsoft.Extensions.Logging.Abstractions 10.0.7
- Microsoft.Extensions.Options 10.0.7

## Установка

Добавьте NuGet пакет в ваш проект или скопируйте исходный код.

## Быстрый старт

### 1. Регистрация в DI контейнере

```csharp
var services = new ServiceCollection();

// Вариант 1: Конфигурация через лямбду
services.AddGptClient(options =>
{
    options.ApiKey = "sk-your-api-key-here";
    options.BaseUrl = "https://api.openai.com/v1";
    options.DefaultModel = "gpt-4";
    options.HttpTimeoutSeconds = 30;
    options.MaxRetryAttempts = 3;
});

// Вариант 2: Конфигурация из IConfiguration
// services.AddGptClient(configuration, "GptClient");
```

### 2. Использование сервиса

```csharp
var gptService = serviceProvider.GetRequiredService<IGptService>();

// Простой запрос
var messages = new List<ChatMessage>
{
    new() { Role = "system", Content = "Ты полезный помощник." },
    new() { Role = "user", Content = "Привет!" }
};

var response = await gptService.SendMessageAsync(messages);
Console.WriteLine(response.Choices[0].Message.Content);
```

### 3. Streaming ответ

```csharp
await foreach (var chunk in gptService.SendMessageStreamAsync(messages))
{
    if (chunk.Choices[0].Delta?.Content != null)
    {
        Console.Write(chunk.Choices[0].Delta.Content);
    }
}
```

## Опции конфигурации

```csharp
public sealed class GptClientOptions
{
    // API ключ для OpenAI-совместимого API
    public required string ApiKey { get; set; }

    // Базовый URL API (например, https://api.openai.com/v1)
    public required string BaseUrl { get; set; }

    // Модель по умолчанию (например, gpt-4, gpt-3.5-turbo)
    public required string DefaultModel { get; set; }

    // Таймаут для HTTP запросов в секундах (по умолчанию: 30)
    public int HttpTimeoutSeconds { get; set; } = 30;

    // Максимальное количество попыток переподключения (по умолчанию: 3)
    public int MaxRetryAttempts { get; set; } = 3;

    // Начальная задержка перед первым ретраем в миллисекундах (по умолчанию: 100)
    public int InitialRetryDelayMs { get; set; } = 100;

    // Максимальная задержка между ретраями в миллисекундах (по умолчанию: 10000)
    public int MaxRetryDelayMs { get; set; } = 10_000;

    // Множитель для exponential backoff (по умолчанию: 2.0)
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    // Максимальное количество запросов в секунду для rate limiting (по умолчанию: 100)
    public int RequestsPerSecond { get; set; } = 100;

    // Статус коды, при которых следует выполнить ретрай
    // (по умолчанию: 408, 429, 500, 502, 503, 504)
    public int[] RetryableStatusCodes { get; set; } = { 408, 429, 500, 502, 503, 504 };
}
```

## Примеры использования

### Пример 1: Простой запрос

```csharp
var messages = new List<ChatMessage>
{
    new() { Role = "user", Content = "Что такое искусственный интеллект?" }
};

var response = await gptService.SendMessageAsync(messages);
Console.WriteLine(response.Choices[0].Message.Content);
```

### Пример 2: Streaming с использованием CancellationToken

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(60));

try
{
    await foreach (var chunk in gptService.SendMessageStreamAsync(
        messages, 
        cancellationToken: cts.Token))
    {
        if (chunk.Choices[0].Delta?.Content != null)
        {
            Console.Write(chunk.Choices[0].Delta.Content);
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Операция отменена пользователем");
}
```

### Пример 3: С параметрами генерации

```csharp
var response = await gptService.SendMessageAsync(
    messages,
    model: "gpt-4",              // Можно переопределить модель
    temperature: 0.7,             // Креативность (0-2, по умолчанию 1.0)
    maxTokens: 200                // Максимальное количество токенов
);
```

### Пример 4: Обработка ошибок

```csharp
try
{
    var response = await gptService.SendMessageAsync(messages);
    Console.WriteLine(response.Choices[0].Message.Content);
}
catch (UnauthorizedGptException ex)
{
    Console.WriteLine($"Ошибка авторизации: {ex.Message}");
}
catch (TemporarilyException ex)
{
    Console.WriteLine($"Сервер временно недоступен: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
}
```

## Архитектура

### Основные компоненты

- **IGptClient** - Низкоуровневый интерфейс для отправки запросов напрямую
- **IGptService** - Высокоуровневый сервис с удобным API
- **ClientBase** - Базовый класс для обработки HTTP запросов и ответов
- **RetryHandler** - Обработчик ретраев с exponential backoff
- **RateLimiter** - Ограничитель скорости запросов
- **GptClientOptions** - Конфигурация клиента
- **IGptServiceFactory** - Фабрика для создания сервиса

### Поток обработки запроса

```
SendMessageAsync()
    ↓
RetryHandler.ExecuteAsync()
    ↓
RateLimiter.AcquireAsync()
    ↓
ClientBase.SendAsync()
    ↓
HTTP запрос к API
    ↓
Обработка ответа
    ↓
JSON десериализация
    ↓
Возврат результата
```

## Обработка ошибок

Клиент автоматически обрабатывает следующие типы ошибок:

- **UnauthorizedGptException** - ошибка авторизации (401)
- **TemporarilyException** - временная ошибка сервера (5xx, сетевые ошибки)
- **OperationCanceledException** - операция отменена через CancellationToken
- **Exception** - прочие ошибки (4xx, 5xx, проблемы с сетью)

Для временных ошибок автоматически выполняется ретрай с exponential backoff.

## Rate Limiting

Клиент использует `System.Threading.RateLimiting.SlidingWindowRateLimiter` для управления скоростью запросов:

- **Default:** 100 запросов в секунду
- **Queue:** 2x от лимита
- **Timeout:** 30 секунд по умолчанию

## Производительность

- **Streaming:** Минимальная задержка между чанками (зависит от сетевого соединения)
- **Retry:** Exponential backoff минимизирует нагрузку на сервер
- **Rate Limiting:** Предотвращает блокировку по лимитам API

## Примеры кода

Полные примеры находятся в файле `Examples/UsageExamples.cs`

## Лицензия

MIT

## Благодарности

Создано с использованием лучших практик:
- System.Threading.RateLimiting для rate limiting
- Microsoft.Extensions для DI и конфигурации
- Async/await для асинхронности
- StreamReader для парсинга SSE потоков

## Поддержка

Для вопросов и проблем создавайте issue в репозитории проекта.
