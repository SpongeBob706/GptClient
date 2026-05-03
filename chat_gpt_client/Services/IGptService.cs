using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Models;

namespace GptClient.Services;

/// <summary>
/// Сервис для обращения к GPT API
/// </summary>
public interface IGptService : IAsyncDisposable
{
    /// <summary>
    /// Отправить сообщение для обработки (без streaming)
    /// </summary>
    Task<ChatCompletionResponse> SendMessageAsync(
        IEnumerable<ChatMessage> messages,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправить сообщение для обработки с потоковым ответом
    /// </summary>
    IAsyncEnumerable<StreamingChatCompletionChunk> SendMessageStreamAsync(
        IEnumerable<ChatMessage> messages,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить текущие лимиты на использование API
    /// </summary>
    RateLimit? GetCurrentRateLimits();
}
