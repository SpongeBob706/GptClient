namespace GptClient.Models;

/// <summary>
/// Опции конфигурации для GPT API клиента
/// </summary>
public sealed class GptClientOptions
{
    /// <summary>
    /// API ключ для OpenAI-совместимого API
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Базовый URL API (например, https://api.openai.com/v1)
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Модель по умолчанию (например, gpt-4, gpt-3.5-turbo)
    /// </summary>
    public required string DefaultModel { get; set; }

    /// <summary>
    /// Модель по умолчанию для изображений (например, gpt-4, gpt-3.5-turbo)
    /// </summary>
    public required string DefaultImageModel { get; set; }

    /// <summary>
    /// Таймаут для HTTP запросов в секундах
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Максимальное количество попыток переподключения
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Начальная задержка перед первым ретраем в миллисекундах
    /// </summary>
    public int InitialRetryDelayMs { get; set; } = 100;

    /// <summary>
    /// Максимальная задержка между ретраями в миллисекундах
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 10_000;

    /// <summary>
    /// Множитель для exponential backoff
    /// </summary>
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Максимальное количество запросов в секунду (для rate limiting)
    /// </summary>
    public int RequestsPerSecond { get; set; } = 100;
}
