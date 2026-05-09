using System.Threading.Tasks;
using GptClient.Client.Impl;
using GptClient.Core;
using GptClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using ChatClient = GptClient.Client.Impl.ChatClient;
using ImageClient = OpenAI.Images.ImageClient;

namespace GptClient.Client;

/// <summary>
/// Основной фасад OpenAI клиента
/// </summary>
internal sealed class OpenAiClient : IOpenAiClient
{
    /// <inheritdoc cref="OpenAiClient" />
    public OpenAiClient(
        IOptions<GptClientOptions> clientOptions,
        IAiPipeline pipeline,
        ILoggerFactory loggerFactory
    )
    {
        var opt = clientOptions.Value;
        var chatSdkClient = new OpenAI.Chat.ChatClient(opt.DefaultModel, opt.ApiKey);

        Chat = new ChatClient(chatSdkClient, pipeline);
        Vision = new VisionClient(chatSdkClient, pipeline, loggerFactory.CreateLogger<VisionClient>());

        var imageSdkClient = new ImageClient(opt.DefaultImageModel, opt.ApiKey);
        Images = new Impl.ImageClient(imageSdkClient, pipeline, loggerFactory.CreateLogger<Impl.ImageClient>());
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
