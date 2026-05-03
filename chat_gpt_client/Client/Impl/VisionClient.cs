using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace GptClient.Client.Impl;

/// <summary>
/// Реализация Vision клиента (работа с изображениями через Chat API)
/// </summary>
internal sealed class VisionClient : IVisionClient
{
    private readonly OpenAI.Chat.ChatClient _client;
    private readonly ILogger _logger;

    /// <inheritdoc cref="VisionClient" />
    public VisionClient(OpenAI.Chat.ChatClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ChatCompletion> AnalyzeImageAsync(
        byte[] imageBytes,
        string prompt,
        string mimeType = "image/png",
        CancellationToken cancellationToken = default)
    {
        var messages = new ChatMessage[]
        {
            new UserChatMessage(
                ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), mimeType),
                ChatMessageContentPart.CreateTextPart(prompt)
            )
        };

        _logger.LogDebug(
            "Analyzing image from bytes. Prompt length: {Length}",
            prompt.Length);

        return await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ChatCompletion> AnalyzeImageFromUrlAsync(
        Uri imageUrl,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var messages = new ChatMessage[]
        {
            new UserChatMessage(
                ChatMessageContentPart.CreateImagePart(imageUrl),
                ChatMessageContentPart.CreateTextPart(prompt)
            )
        };

        _logger.LogDebug("Analyzing image from URL: {Url}", imageUrl);

        return await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
    }
}
