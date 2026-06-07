using System;
using System.Collections.Generic;
using OpenAI.Responses;

namespace GptClient.Models;

/// <summary>
/// Универсальный запрос к Responses API.
/// </summary>
public sealed class ResponseRequest
{
    /// <summary>
    /// Сессия.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Пользовательский промпт.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Продолжать предыдущий диалог.
    /// </summary>
    public bool ContinueConversation { get; init; } = true;

    /// <summary>
    /// Изображения.
    /// </summary>
    public IReadOnlyCollection<ResponseImage> Images { get; init; } = Array.Empty<ResponseImage>();

    public ImageGenerationToolQuality? Quality { get; init; }
    public ImageGenerationToolSize? Size { get; init; }
}
