using System.Threading.Tasks;
using GptClient.Client.Impl;
using Microsoft.Extensions.Logging;
using ChatClient = GptClient.Client.Impl.ChatClient;

namespace GptClient.Client;

/// <summary>
/// Основной фасад OpenAI клиента
/// </summary>
internal sealed class OpenAiClient : IOpenAiClient
{
    /// <inheritdoc cref="OpenAiClient" />
    public OpenAiClient(
        string apiKey,
        ILoggerFactory loggerFactory,
        RetryHandler retryHandler,
        RateLimiter rateLimiter)
    {
        var logger = loggerFactory.CreateLogger("OpenAI");

        var chatSdkClient = new OpenAI.Chat.ChatClient("gpt-4.1", apiKey);

        Chat = new ChatClient(chatSdkClient);
        Vision = new VisionClient(chatSdkClient, loggerFactory.CreateLogger<VisionClient>());
        Images = new ImageClient("gpt-4.1", apiKey, loggerFactory.CreateLogger<ImageClient>());
    }

    /// <inheritdoc />
    public IChatClient Chat { get; }

    /// <inheritdoc />
    public IVisionClient Vision { get; }

    /// <inheritdoc />
    public IImageClient Images { get; }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
