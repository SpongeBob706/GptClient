using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Core;
using GptClient.Models;
using OpenAI.Chat;

namespace GptClient.Client.Impl;

/// <summary>
/// Chat клиент с использованием pipeline
/// </summary>
internal sealed class ChatClient : IChatClient
{
    private readonly OpenAI.Chat.ChatClient _client;
    private readonly IAiPipeline _pipeline;

    /// <inheritdoc cref="ChatClient" />
    public ChatClient(
        OpenAI.Chat.ChatClient client,
        IAiPipeline pipeline)
    {
        _client = client;
        _pipeline = pipeline;
    }

    /// <inheritdoc />
    public Task<ChatCompletion> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken)
    {
        return _pipeline.ExecuteAsync<ChatCompletion>(
            new AiContext
            {
                OperationName = "ChatCompletion",
                Request = new ChatExecutionRequest(messages, options),
            },
            async (ctx, ct) =>
            {
                var req = (ChatExecutionRequest)ctx.Request!;

                var result = await _client.CompleteChatAsync(req.Messages, req.Options, ct);

                return result.Value;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<StreamingChatCompletionUpdate> StreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken)
    {
        // streaming обычно НЕ оборачивают retry (важное архитектурное решение)
        return _client.CompleteChatStreamingAsync(messages, options, cancellationToken);
    }
}
