using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GptClient.Client.Impl;

/// <summary>
/// Реализация клиента генерации изображений
/// </summary>
internal sealed class ImageClient : IImageClient
{
    private readonly OpenAI.Images.ImageClient _client;
    private readonly ILogger _logger;

    /// <inheritdoc cref="ImageClient" />
    public ImageClient(string model, string apiKey, ILogger logger)
    {
        _client = new OpenAI.Images.ImageClient(model: model, apiKey: apiKey);
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

        var result = await _client.GenerateImageAsync(
            prompt,
            cancellationToken: cancellationToken);

        return result.Value.ImageUri.ToString();
    }
}
