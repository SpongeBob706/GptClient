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
    /// Модель по умолчанию (например, gpt-5.5)
    /// </summary>
    public required string DefaultModel { get; set; } = "gpt-5.5";

    /// <summary>
    /// Модель по умолчанию для изображений (например, gpt-image-2)
    /// </summary>
    /// <remarks>
    /// https://developers.openai.com/api/docs/guides/tools-image-generation#supported-models
    /// </remarks>
    public required string DefaultImageModel { get; set; } = "gpt-image-2";

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

    /// <summary>
    /// Адрес прокси-сервера (например, http://proxy.company.com:8080)
    /// </summary>
    public string? ProxyAddress { get; set; }

    /// <summary>
    /// Имя пользователя для аутентификации прокси
    /// </summary>
    public string? ProxyUsername { get; set; }

    /// <summary>
    /// Пароль для аутентификации прокси
    /// </summary>
    public string? ProxyPassword { get; set; }

    /// <summary>
    /// Обходить прокси для локальных адресов
    /// </summary>
    public bool ProxyBypassLocal { get; set; } = true;
}
