namespace GptClient.Models;

/// <summary>
/// Ограничения скорости для ChatGPT
/// </summary>
/// <remarks>https://platform.openai.com/docs/guides/rate-limits?context=tier-free#rate-limits-in-headers</remarks>
public sealed class RateLimit
{
    /// <summary>
    /// Максимально допустимое количество запросов до исчерпания лимита скорости.
    /// </summary>
    public int? LimitRequests { get; set; }

    /// <summary>
    /// Максимально допустимое количество токенов до исчерпания лимита скорости.
    /// </summary>
    public int? LimitTokens { get; set; }

    /// <summary>
    /// Оставшееся количество запросов, разрешенных до исчерпания лимита скорости.
    /// </summary>
    public int? RemainingRequests { get; set; }

    /// <summary>
    /// Оставшееся количество токенов, разрешенное до исчерпания лимита скорости.
    /// </summary>
    public int? RemainingTokens { get; set; }

    /// <summary>
    /// Время, пока ограничение скорости (на основе запросов) не вернется в исходное состояние.
    /// </summary>
    public int? ResetRequests { get; set; }

    /// <summary>
    /// Время, пока ограничение скорости (на основе токенов) не вернется к исходному состоянию.
    /// </summary>
    public int? ResetTokens { get; set; }
}
