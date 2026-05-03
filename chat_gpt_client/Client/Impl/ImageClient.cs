using System.Threading;
using System.Threading.Tasks;
using GptClient.Core;
using Microsoft.Extensions.Logging;

namespace GptClient.Client.Impl;

/// <summary>
/// Реализация клиента генерации изображений
/// </summary>
internal sealed class ImageClient : IImageClient
{
    private readonly OpenAI.Images.ImageClient _client;
    private readonly IAiPipeline _pipeline;
    private readonly ILogger _logger;

    /// <inheritdoc cref="ImageClient" />
    public ImageClient(
        OpenAI.Images.ImageClient client,
        IAiPipeline pipeline,
        ILogger logger)
    {
        _client = client;
        _pipeline = pipeline;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Generating image. Prompt length: {Length}",
            prompt.Length);

        return await _pipeline.ExecuteAsync<string>(
            new AiContext
            {
                OperationName = "ImageGenerating",
                Request = prompt
            },
            async (ctx, ct) =>
            {
                var req = (string)ctx.Request!;

                var result = await _client.GenerateImageAsync(
                    req,
                    cancellationToken: ct);

                return result.Value.ImageUri.ToString();
            },
            cancellationToken);
    }
}
