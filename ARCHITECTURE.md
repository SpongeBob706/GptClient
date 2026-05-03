# Архитектура GPT API Client

## Обзор

GPT API Client - это полнофункциональный асинхронный клиент для работы с OpenAI-совместимыми API, построенный на основе современных паттернов C# с использованием Dependency Injection, асинхронного программирования и потокобезопасных операций.

## Структура проекта

```
chat_gpt_client/
├── Client/
│   ├── IGptClient.cs                 # Интерфейс низкоуровневого клиента
│   └── Impl/
│       ├── ClientBase.cs             # Базовый класс для HTTP операций
│       ├── GptClient.cs              # Реализация IGptClient
│       ├── RetryHandler.cs           # Обработчик ретраев с exponential backoff
│       └── RateLimiter.cs            # Ограничитель скорости запросов
├── Services/
│   ├── IGptService.cs                # Интерфейс высокоуровневого сервиса
│   └── Impl/
│       └── GptService.cs             # Реализация IGptService
├── Factories/
│   ├── IGptServiceFactory.cs         # Интерфейс фабрики
│   └── Impl/
│       └── GptServiceFactory.cs      # Реализация фабрики
├── Models/
│   ├── GptClientOptions.cs           # Конфигурация клиента
│   ├── ChatModels.cs                 # Модели для запросов/ответов
│   └── RateLimits.cs                 # Модель ограничений скорости
├── Extensions/
│   ├── DisplayExtension.cs           # Расширения для отображения
│   ├── JsonConvertExtensions.cs      # Расширения для JSON
│   └── StringExtensions.cs           # Расширения для строк
├── Exceptions/
│   ├── TemporarilyException.cs       # Временные ошибки
│   └── UnauthorizedGptException.cs   # Ошибки авторизации
├── ServiceCollectionExtensions.cs    # Регистрация в DI контейнере
├── Examples/
│   └── UsageExamples.cs              # Примеры использования
└── AspNetCoreIntegration.cs          # Примеры интеграции в ASP.NET Core
```

## Слои архитектуры

### 1. Уровень клиента (Client Layer)

**Ответственность:** Низкоуровневая работа с HTTP и потоками данных

#### ClientBase
```csharp
abstract class ClientBase
    - SendAsync<T>()              // Отправка и получение типизированного ответа
    - SendStreamAsync<T>()        // Потоковая передача данных (SSE)
    - SendImplAsync()             // Внутренняя реализация HTTP запроса
```

**Особенности:**
- Работа с `HttpRequestMessage` для гибкости
- Обработка JSON и других типов контента
- Парсинг SSE (Server-Sent Events) потоков
- Пробрасывание специфичных исключений

#### GptClient : ClientBase, IGptClient
```csharp
class GptClient
    - SendChatCompletionAsync()   // Простой запрос к API
    - StreamChatCompletionAsync() // Потоковый запрос к API
```

**Взаимодействия:**
- Использует `RetryHandler` для обработки ошибок
- Использует `RateLimiter` для ограничения скорости
- Вызывает базовые методы из `ClientBase`

#### RetryHandler
```csharp
class RetryHandler
    - ExecuteAsync<T>()           // Выполнение операции с ретраями
    - IsRetryableStatusCode()     // Проверка кода ошибки
```

**Логика:**
```
Попытка N
    ├─ Успех → возврат результата
    └─ TemporarilyException (код 5xx/сеть)
       ├─ N < MaxAttempts
       │  └─ Ждём delay (exponential backoff)
       │     └─ Переходим к попытке N+1
       └─ N >= MaxAttempts
          └─ Пробрасываем исключение
```

**Параметры exponential backoff:**
```
delay_n = min(initial_delay * (multiplier ^ (n-1)), max_delay)
Пример: 100ms → 200ms → 400ms → 800ms → 1600ms (max 10s)
```

#### RateLimiter
```csharp
class RateLimiter : IAsyncDisposable
    - AcquireAsync()              // Получить разрешение на запрос
    - DisposeAsync()              // Освобождение ресурсов
```

**Реализация:**
- Использует `System.Threading.RateLimiting.SlidingWindowRateLimiter`
- Асинхронное ожидание при превышении лимита
- Автоматическое восстановление разрешений

### 2. Уровень сервиса (Service Layer)

**Ответственность:** Высокоуровневый API для приложения

#### IGptService
```csharp
interface IGptService
    - SendMessageAsync()          // Отправить сообщение
    - SendMessageStreamAsync()    // Потоковый ответ
    - GetCurrentRateLimits()      // Получить текущие лимиты
```

#### GptService : IGptService
```csharp
class GptService
    - BuildRequest()              // Формирование запроса
    - ThrowIfDisposed()          // Проверка состояния
```

**Обязанности:**
- Конвертация между моделями приложения и API
- Управление логированием
- Обработка ошибок и их логирование
- Очистка ресурсов

### 3. Уровень фабрики (Factory Layer)

**Ответственность:** Создание экземпляров сервиса с правильными зависимостями

#### IGptServiceFactory
```csharp
interface IGptServiceFactory
    - Create() : IGptService     // Создание сервиса
```

#### GptServiceFactory : IGptServiceFactory
```csharp
class GptServiceFactory
    - Create()                    // Инстанцирование всех зависимостей
```

**Создаёт граф объектов:**
```
GptServiceFactory
    ├─ создаёт RetryHandler (из конфига)
    ├─ создаёт RateLimiter (из конфига)
    ├─ создаёт GptClient (с выше созданными объектами)
    └─ создаёт GptService (оборачивает GptClient)
```

### 4. Модельный уровень (Models Layer)

#### GptClientOptions
```csharp
class GptClientOptions
    - ApiKey                      // Ключ API
    - BaseUrl                     // URL сервера
    - DefaultModel                // Модель по умолчанию
    - HttpTimeoutSeconds          // Таймаут HTTP
    - MaxRetryAttempts            // Макс. попыток
    - InitialRetryDelayMs         // Начальная задержка
    - MaxRetryDelayMs             // Макс. задержка
    - RetryBackoffMultiplier      // Множитель backoff
    - RequestsPerSecond           // Лимит запросов
    - RetryableStatusCodes[]      // Коды для ретрая
```

#### Chat Models
```
ChatMessage          - Сообщение пользователя/AI
ChatCompletionRequest - Запрос к API
ChatCompletionResponse - Ответ от API
ChatCompletionChoice - Выбор в ответе
ChatCompletionUsage  - Информация об использовании токенов

StreamingChatCompletionChunk - Кусок потока
StreamingChoice              - Выбор в потоке
DeltaMessage                - Дельта изменения

ErrorResponse        - Ошибка от API
ErrorDetail          - Деталь ошибки
```

## Поток обработки запроса

### Non-Streaming запрос

```
User Code
    ↓
IGptService.SendMessageAsync(messages)
    ↓ (преобразование в ChatCompletionRequest)
    ↓
IGptClient.SendChatCompletionAsync(request)
    ↓
RetryHandler.ExecuteAsync()
    ├─ RateLimiter.AcquireAsync()    # Ожидаем разрешения
    ├─ HttpClient.SendAsync()        # Отправляем запрос
    └─ ClientBase.SendAsync<T>()     # Получаем и десериализуем ответ
    ↓ (при ошибке 5xx - ретрай)
    ↓
ChatCompletionResponse
    ↓
Возврат в User Code
```

### Streaming запрос

```
User Code
    ↓
IGptService.SendMessageStreamAsync(messages)
    ↓
IGptClient.StreamChatCompletionAsync(request)
    ↓
ClientBase.SendStreamAsync<T>()
    ├─ HttpClient.SendAsync()
    ├─ Получение потока
    ├─ Построчное чтение SSE
    └─ Парсинг JSON chunks
    ↓
IAsyncEnumerable<StreamingChatCompletionChunk>
    ↓
foreach - yield на каждый chunk
    ├─ RateLimiter.AcquireAsync()    # Ограничение на chunk
    └─ Возврат chunk
    ↓
Завершение или отмена (CancellationToken)
```

## Обработка ошибок

```
HTTP Response
    │
    ├─ 2xx (Success)
    │  └─ Десериализация и возврат
    │
    ├─ 401 (Unauthorized)
    │  └─ throw UnauthorizedGptException
    │
    ├─ 4xx (Client Error)
    │  └─ throw Exception (перехватываемо ClientBase)
    │
    ├─ 5xx (Server Error)
    │  └─ throw TemporarilyException
    │     └─ RetryHandler.ExecuteAsync()
    │        ├─ Ретрай с exponential backoff
    │        └─ Если успех - возврат результата
    │        └─ Если fail - пробросить исключение
    │
    └─ Network/IO Error
       └─ throw TemporarilyException (обёрнуто)
          └─ RetryHandler обработает
```

## Жизненный цикл объектов

### Создание
```
ServiceCollectionExtensions.AddGptClient()
    ├─ Configure<GptClientOptions>()
    ├─ AddHttpClient()
    ├─ AddSingleton(IGptServiceFactory, GptServiceFactory)
    └─ AddSingleton(IGptService, factory => factory.Create())
```

### Инициализация
```
GptServiceFactory.Create()
    ├─ new RetryHandler()
    ├─ new RateLimiter()
    ├─ new GptClient()
    └─ new GptService() → возвращаемый объект
```

### Использование
```
IGptService.SendMessageAsync()
    ├─ RateLimiter.AcquireAsync()  # Блокирует если нужно
    ├─ IGptClient.SendAsync()       # Отправляет с ретраями
    └─ Возврат результата
```

### Очистка ресурсов
```
await gptService.DisposeAsync()
    └─ await gptClient.DisposeAsync()
        └─ await rateLimiter.DisposeAsync()
            └─ rateLimiter.Dispose()
```

## Безопасность и производительность

### Thread Safety
- ✅ `RateLimiter` потокобезопасен (использует блокировки внутри)
- ✅ `HttpClient` создается через `IHttpClientFactory` (переиспользование соединений)
- ✅ Все операции асинхронные (не блокируют потоки)

### Оптимизация памяти
- ✅ Streaming использует `IAsyncEnumerable` (не загружает весь ответ в память)
- ✅ `using` для HttpRequestMessage и StreamReader
- ✅ `GC.SuppressFinalize()` для правильной очистки

### Надёжность
- ✅ Exponential backoff минимизирует нагрузку на сервер при ошибках
- ✅ Rate limiting предотвращает блокировку по лимитам API
- ✅ Специфичные исключения для разных типов ошибок
- ✅ CancellationToken для отмены операций

## Примеры вызовов

### Простой запрос
```csharp
var response = await service.SendMessageAsync(messages);
```

### Потоковый запрос
```csharp
await foreach (var chunk in service.SendMessageStreamAsync(messages))
{
    Console.Write(chunk.Choices[0].Delta?.Content);
}
```

### С отменой
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await service.SendMessageAsync(messages, cancellationToken: cts.Token);
```

### С параметрами
```csharp
var response = await service.SendMessageAsync(
    messages,
    model: "gpt-4",
    temperature: 0.7,
    maxTokens: 200
);
```

## Расширяемость

Архитектура позволяет легко добавить:

1. **Другие методы API** - добавить методы в `IGptClient` и `IGptService`
2. **Кеширование** - обернуть `GptService` в декоратор
3. **Метрики** - добавить логирование в `RetryHandler` и `RateLimiter`
4. **Интеграция с Message Broker** - использовать `IGptService` как backend
5. **Поддержка других OpenAI эндпоинтов** - расширить `ClientBase`

## Зависимости

Минимальный набор:
- `System` (встроен)
- `System.Threading.RateLimiting` (встроен в .NET 10)
- `Microsoft.Extensions.*` (для DI и конфигурации)

Нет зависимостей от OpenAI SDK или других сторонних библиотек!
