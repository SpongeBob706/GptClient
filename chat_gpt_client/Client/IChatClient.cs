using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace GptClient.Client;

/// <summary>
/// Клиент для работы с Chat API
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Выполняет синхронный запрос к chat API
    /// </summary>
    Task<ChatCompletion> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет потоковый запрос к chat API
    /// </summary>
    IAsyncEnumerable<StreamingChatCompletionUpdate> StreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken);
}
