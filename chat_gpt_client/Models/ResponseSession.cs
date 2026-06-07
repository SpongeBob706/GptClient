using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GptClient.Models;

/// <summary>
/// Состояние одной Responses API сессии.
/// </summary>
public sealed class ResponseSession
{
    /// <summary>
    /// Идентификатор пользовательской сессии.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Последний response_id от OpenAI.
    /// Используется для previous_response_id.
    /// </summary>
    public string? LastResponseId { get; set; }

    /// <summary>
    /// Дополнительные данные,
    /// которые может использовать бизнес-логика.
    /// Например ссылки на изображения.
    /// </summary>
    public ConcurrentDictionary<string, object> Metadata { get; } = new();
}
