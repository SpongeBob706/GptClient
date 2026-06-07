using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Client;
using GptClient.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI.Images;

namespace GptClient.Services.Impl;

/// <summary>
/// Реализация сервиса для работы с GPT API
/// </summary>
internal sealed class GptService : IGptService
{
    private readonly IOpenAiClient _openAiClient;
    private readonly ILogger<GptService> _logger;
    private bool _disposed;

    private const string DefaultSystemMessage =
        "You are a helpful, accurate, and thoughtful assistant. Provide clear and direct responses.";

    private const string TextEditorSystemMessage = "You are a precise text editing assistant. " +
        "Your only job is to apply the given instruction to the provided text. " +
        "Return only the transformed text without any explanations, quotes, or markdown.";

    private const string ImageAnalysisPrompt =
        "Describe this image in maximum detail. Include all visual elements: objects, people, colors, " +
        "lighting, composition, style, mood, background, and any text visible. Be extremely thorough.";

    public GptService(IOpenAiClient openAiClient, ILogger<GptService> logger)
    {
        _openAiClient = openAiClient ?? throw new ArgumentNullException(nameof(openAiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Responses

    /// <inheritdoc />
    public async Task<ResponseExecutionResult> ExecuteResponseAsync(
        ResponseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        LogOperationStarted(
            "Responses API",
            ("SessionId", request.SessionId));

        try
        {
            var result = await _openAiClient.ResponseImageClient.ExecuteAsync(
                request,
                cancellationToken);

            LogOperationCompleted(
                "Responses API",
                ("ResponseId", result.ResponseId));

            return result;
        }
        catch (OperationCanceledException)
        {
            LogOperationCancelled("Responses API");
            throw;
        }
        catch (Exception ex)
        {
            LogOperationFailed("Responses API", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ResponseExecutionResult> ContinueResponseAsync(
        string sessionId,
        string prompt,
        IReadOnlyCollection<ResponseImage>? images = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(prompt);

        return await ExecuteResponseAsync(
            new ResponseRequest
            {
                SessionId = sessionId,
                Prompt = prompt,
                ContinueConversation = true,
                Images = images ?? Array.Empty<ResponseImage>()
            },
            cancellationToken);
    }

  #endregion

    #region Text Generation

    /// <inheritdoc />
    public async Task<string> GenerateTextAsync(
        string prompt,
        string? systemMessage = null,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(prompt);

        LogOperationStarted("Text generation", ("PromptLength", prompt.Length));

        var messages = BuildMessages(prompt, systemMessage);
        var completion = await ExecuteChatCompletionAsync(
            messages,
            operationName: "text generation",
            responseFormat: responseFormat,
            cancellationToken: cancellationToken);

        return ExtractText(completion);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GenerateTextStreamAsync(
        string prompt,
        string? systemMessage = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateInput(prompt);

        LogOperationStarted("Streaming text generation", ("PromptLength", prompt.Length));

        var messages = BuildMessages(prompt, systemMessage);

        await foreach (var chunk in ExecuteStreamingAsync(messages, cancellationToken))
        {
            yield return chunk;
        }

        _logger.LogDebug("Streaming text generation completed");
    }

    #endregion

    #region Text Transformation

    /// <inheritdoc />
    public async Task<string> TransformTextAsync(
        string text,
        string instruction,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(text, nameof(text));
        ValidateInput(instruction, nameof(instruction));

        LogOperationStarted("Text transformation",
            ("TextLength", text.Length),
            ("Instruction", instruction));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(TextEditorSystemMessage),
            new UserChatMessage(
                $"Original text:\n\"{text}\"\n\n" +
                $"Instruction: {instruction}\n\n" +
                "Transformed text:")
        };

        var completion = await ExecuteChatCompletionAsync(
            messages,
            operationName: "text transformation",
            cancellationToken,
            responseFormat: responseFormat);

        return ExtractText(completion);
    }

    /// <inheritdoc />
    public async Task<string> TransformTextWithImageContextAsync(
        string text,
        byte[] imageBytes,
        string instruction,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(text, nameof(text));
        ValidateImageBytes(imageBytes);
        ValidateInput(instruction, nameof(instruction));

        LogOperationStarted("Text transformation with image context",
            ("TextLength", text.Length),
            ("ImageSize", imageBytes.Length));

        var imageDescription = await AnalyzeImageInternalAsync(
            imageBytes,
            "Describe this image concisely but comprehensively, focusing on elements relevant to text context.",
            cancellationToken);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are an assistant that transforms text based on image context. " +
                "Use the image description to understand the visual context, then apply the instruction to the text. " +
                "Return only the transformed text without explanations."),
            new UserChatMessage(
                $"Image context: {imageDescription}\n\n" +
                $"Original text: \"{text}\"\n\n" +
                $"Instruction: {instruction}\n\n" +
                "Transformed text:")
        };

        var completion = await ExecuteChatCompletionAsync(
            messages,
            operationName: "text transformation with image context",
            cancellationToken,
            responseFormat: responseFormat);

        return ExtractText(completion);
    }

    #endregion

    #region Dialogue

    /// <inheritdoc />
    public async Task<string> ContinueDialogueAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messagesList = messages.ToList();
        if (messagesList.Count == 0)
            throw new ArgumentException("Список сообщений не может быть пустым", nameof(messages));

        LogOperationStarted("Dialogue continuation", ("MessageCount", messagesList.Count));

        var completion = await ExecuteChatCompletionAsync(
            messagesList,
            operationName: "dialogue continuation",
            responseFormat: responseFormat,
            cancellationToken: cancellationToken);

        return ExtractText(completion);
    }

    /// <summary>
    /// Продолжает диалог и возвращает полный ChatCompletion с возможными изображениями
    /// </summary>
    public async Task<ChatCompletion> ContinueDialogueWithImagesAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messagesList = messages.ToList();
        if (messagesList.Count == 0)
            throw new ArgumentException("Список сообщений не может быть пустым", nameof(messages));

        LogOperationStarted("Dialogue continuation with images", ("MessageCount", messagesList.Count));

        return await ExecuteChatCompletionAsync(
            messagesList,
            operationName: "dialogue continuation with images",
            responseFormat: responseFormat,
            cancellationToken: cancellationToken);
    }

    #endregion

    #region Image Operations

    /// <inheritdoc />
    public async Task<byte[]> EditImageWithAnalyseAsync(
        byte[] imageBytes,
        string editPrompt,
        CancellationToken cancellationToken = default)
    {
        ValidateImageBytes(imageBytes);
        ValidateInput(editPrompt, nameof(editPrompt));

        LogOperationStarted("Image editing",
            ("ImageSize", $"{imageBytes.Length} bytes"),
            ("EditPrompt", editPrompt));

        _logger.LogDebug("Step 1/2: Analyzing source image");
        var description = await AnalyzeImageInternalAsync(imageBytes, ImageAnalysisPrompt, cancellationToken);

        _logger.LogDebug("Step 2/2: Generating new image");
        var generationPrompt = BuildImageEditPrompt(description, editPrompt);

        return await ExecuteImageGenerationAsync(generationPrompt, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> EditImageDirectAsync(
        byte[] imageBytes,
        string editPrompt,
        GeneratedImageQuality? quality = null,
        GeneratedImageSize? size = null,
        CancellationToken cancellationToken = default)
    {
        ValidateImageBytes(imageBytes);
        ValidateInput(editPrompt, nameof(editPrompt));

        LogOperationStarted(
            "Direct image editing",
            ("ImageSize", imageBytes.Length),
            ("EditPrompt", editPrompt));

        try
        {
            var result = await _openAiClient.Images.EditAsync(
                imageBytes,
                editPrompt,
                quality,
                size,
                cancellationToken);

            LogOperationCompleted("Direct image editing");

            return result;
        }
        catch (OperationCanceledException)
        {
            LogOperationCancelled(
                "direct image editing");

            throw;
        }
        catch (Exception ex)
        {
            LogOperationFailed(
                "direct image editing",
                ex);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> EditImageFromUrlAsync(
        Uri imageUrl,
        string editPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageUrl);
        ValidateInput(editPrompt, nameof(editPrompt));

        LogOperationStarted("Image editing from URL", ("ImageUrl", imageUrl.ToString()));

        _logger.LogDebug("Step 1/2: Analyzing image from URL");
        var description = await AnalyzeImageFromUrlInternalAsync(
            imageUrl,
            "Describe this image in maximum detail including all visual elements, style, and composition.",
            cancellationToken);

        _logger.LogDebug("Step 2/2: Generating new image");
        var generationPrompt = BuildImageEditPrompt(description, editPrompt);

        return await ExecuteImageGenerationAsync(generationPrompt, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> DescribeImageAsync(
        byte[] imageBytes,
        string? analysisPrompt = null,
        CancellationToken cancellationToken = default)
    {
        ValidateImageBytes(imageBytes);

        var prompt = analysisPrompt ?? "Provide a detailed and comprehensive description of this image.";

        LogOperationStarted("Image analysis", ("ImageSize", $"{imageBytes.Length} bytes"));

        return await AnalyzeImageInternalAsync(imageBytes, prompt, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateImageAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(prompt, nameof(prompt));

        LogOperationStarted("Image generation", ("Prompt", prompt));

        return await ExecuteImageGenerationAsync(prompt, cancellationToken);
    }

    #endregion

    #region Core Execution Methods

    private async Task<ChatCompletion> ExecuteChatCompletionAsync(
        List<ChatMessage> messages,
        string operationName,
        CancellationToken cancellationToken,
        ChatResponseFormat? responseFormat = null)
    {
        try
        {
            var options = new ChatCompletionOptions { ResponseFormat = responseFormat };

            var completion = await _openAiClient.Chat.CompleteAsync(messages, options, cancellationToken);

            LogOperationCompleted(operationName, ("ResponseLength", completion.Content[0].Text.Length));
            return completion;
        }
        catch (OperationCanceledException)
        {
            LogOperationCancelled(operationName);
            throw;
        }
        catch (Exception ex)
        {
            LogOperationFailed(operationName, ex);
            throw;
        }
    }

    /// <summary>
    /// Извлекает текст из ChatCompletion
    /// </summary>
    private static string ExtractText(ChatCompletion completion)
    {
        return completion.Content[0].Text;
    }

    private async IAsyncEnumerable<string> ExecuteStreamingAsync(
        List<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in _openAiClient.Chat.StreamAsync(
            messages,
            new ChatCompletionOptions(),
            cancellationToken))
        {
            if (update.ContentUpdate.Count > 0)
            {
                var chunk = update.ContentUpdate[0].Text;
                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk;
            }
        }
    }

    private async Task<string> AnalyzeImageInternalAsync(
        byte[] imageBytes,
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _openAiClient.Vision.AnalyzeImageAsync(
                imageBytes, prompt, cancellationToken: cancellationToken);

            var description = result.Content[0].Text;
            LogOperationCompleted("image analysis", ("DescriptionLength", description.Length));
            return description;
        }
        catch (OperationCanceledException)
        {
            LogOperationCancelled("image analysis");
            throw;
        }
        catch (Exception ex)
        {
            LogOperationFailed("image analysis", ex);
            throw;
        }
    }

    private async Task<string> AnalyzeImageFromUrlInternalAsync(
        Uri imageUrl,
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _openAiClient.Vision.AnalyzeImageFromUrlAsync(
                imageUrl, prompt, cancellationToken);

            var description = result.Content[0].Text;
            LogOperationCompleted("image analysis from URL", ("DescriptionLength", description.Length));
            return description;
        }
        catch (OperationCanceledException)
        {
            LogOperationCancelled("image analysis from URL");
            throw;
        }
        catch (Exception ex)
        {
            LogOperationFailed("image analysis from URL", ex);
            throw;
        }
    }

    private async Task<byte[]> ExecuteImageGenerationAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var imageBytes = await _openAiClient.Images.GenerateAsync(prompt, cancellationToken);
            LogOperationCompleted("image generation");
            return imageBytes;
        }
        catch (OperationCanceledException)
        {
            LogOperationCancelled("image generation");
            throw;
        }
        catch (Exception ex)
        {
            LogOperationFailed("image generation", ex);
            throw;
        }
    }

    #endregion

    #region Helpers

    private static List<ChatMessage> BuildMessages(string prompt, string? systemMessage)
    {
        return new List<ChatMessage>
        {
            new SystemChatMessage(
                string.IsNullOrWhiteSpace(systemMessage) ? DefaultSystemMessage : systemMessage),
            new UserChatMessage(prompt)
        };
    }

    private static string BuildImageEditPrompt(string description, string editPrompt)
    {
        return $"Reference image description: {description}\n\n" +
            $"Requested modifications: {editPrompt}\n\n" +
            "Generate a new image that applies the requested modifications while preserving " +
            "the overall composition, subject matter, and style of the reference where appropriate.";
    }

    private static void ValidateInput(string input, string paramName = "prompt")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input, paramName);
    }

    private static void ValidateImageBytes(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            throw new ArgumentException("Массив байтов изображения не может быть пустым", nameof(imageBytes));
    }

    #endregion

    #region Structured Logging

    private void LogOperationStarted(string operation, params (string Key, object Value)[] properties)
    {
        using var scope = _logger.BeginScope(ToDictionary(properties));
        _logger.LogDebug("{Operation}: started", operation);
    }

    private void LogOperationCompleted(string operation, params (string Key, object Value)[] properties)
    {
        using var scope = _logger.BeginScope(ToDictionary(properties));
        _logger.LogInformation("{Operation}: completed successfully", operation);
    }

    private void LogOperationCancelled(string operation)
    {
        _logger.LogWarning("{Operation}: cancelled", operation);
    }

    private void LogOperationFailed(string operation, Exception ex)
    {
        _logger.LogError(ex, "{Operation}: failed", operation);
    }

    private static Dictionary<string, object> ToDictionary(params (string Key, object Value)[] properties)
    {
        return properties.ToDictionary(p => p.Key, p => p.Value);
    }

    #endregion

    #region Disposable

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GptService));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogDebug("Disposing GptService resources");

        if (_openAiClient != null)
        {
            await _openAiClient.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}
