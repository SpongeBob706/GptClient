using System;
using System.ClientModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Core;
using Microsoft.Extensions.Logging;
using OpenAI.Images;

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
    public async Task<byte[]> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Generating image. Prompt length: {Length}",
            prompt.Length);

        return await _pipeline.ExecuteAsync<byte[]>(
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
                    new ImageGenerationOptions(),
                    cancellationToken: ct);

                return result.Value.ImageBytes.ToArray();
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> EditAsync(
        byte[] image,
        string prompt,
        GeneratedImageQuality? quality = null,
        GeneratedImageSize? size = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Editing image. Prompt length: {Length}",
            prompt.Length);

        return await _pipeline.ExecuteAsync<byte[]>(
            new AiContext
            {
                OperationName = "ImageEditing",
                Request = prompt
            },
            async (_, ct) =>
            {
                await using var imageStream = new MemoryStream(image);

                var result = await _client.GenerateImageEditAsync(
                    imageStream,
                    "image.png",
                    prompt,
                    options: new ImageEditOptions
                    {
                        Quality = quality,
                        Size = size
                    },
                    cancellationToken:
                    ct);

                return result.Value.ImageBytes.ToArray();
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> EditAsync(
        byte[] image,
        string prompt,
        byte[] mask,
        GeneratedImageQuality? quality = null,
        GeneratedImageSize? size = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Editing image with mask. Prompt length: {Length}",
            prompt.Length);

        return await _pipeline.ExecuteAsync<byte[]>(
            new AiContext
            {
                OperationName = "ImageEditingWithMask",
                Request = prompt
            },
            async (_, ct) =>
            {
                await using var imageStream = new MemoryStream(image);

                await using var maskStream = new MemoryStream(mask);

                var result = await _client.GenerateImageEditAsync(
                    imageStream,
                    "image.png",
                    prompt,
                    maskStream,
                    "mask.png",
                    options: new ImageEditOptions
                    {
                        Quality = quality,
                        Size = size
                    },
                    cancellationToken: ct);

                return result.Value.ImageBytes.ToArray();
            },
            cancellationToken);
    }
}
