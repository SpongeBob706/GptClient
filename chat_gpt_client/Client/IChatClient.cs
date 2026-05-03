using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace GptClient.Client;

/// <summary>
/// Клиент для работы с текстовыми запросами ChatGPT
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Выполнить chat completion запрос
    /// </summary>
    Task<ChatCompletion> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Выполнить streaming chat completion запрос
    /// </summary>
    IAsyncEnumerable<StreamingChatCompletionUpdate> StreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken);
}
