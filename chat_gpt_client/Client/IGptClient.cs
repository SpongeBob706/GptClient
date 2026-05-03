using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Models;

namespace GptClient.Client;

/// <summary>
/// Клиент для работы с GPT API (OpenAI-совместимый)
/// </summary>
public interface IGptClient : IAsyncDisposable
{
    /// <summary>
    /// Отправить запрос на chat completion без streaming
    /// </summary>
    /// <param name="request">Запрос к API</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ от API</returns>
    Task<ChatCompletionResponse> SendChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправить запрос на chat completion со streaming ответом
    /// </summary>
    /// <param name="request">Запрос к API</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Асинхронный перечислитель чанков ответа</returns>
    IAsyncEnumerable<StreamingChatCompletionChunk> StreamChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}
