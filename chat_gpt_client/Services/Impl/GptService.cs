using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Client;
using GptClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GptClient.Services.Impl;

/// <summary>
/// Реализация сервиса для работы с GPT API
/// </summary>
internal sealed class GptService : IGptService
{
    private readonly IChatClient _chatClient;
    private readonly GptClientOptions _options;
    private readonly ILogger _logger;
    private RateLimit? _currentRateLimit;
    private bool _disposed;

    public GptService(
        IChatClient chatClient,
        IOptions<GptClientOptions> options,
        ILogger<GptService> logger)
    {
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Отправить сообщение для обработки (без streaming)
    /// </summary>
    public async Task<ChatCompletionResponse> SendMessageAsync(
        IEnumerable<ChatMessage> messages,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var request = BuildRequest(messages, model, temperature, maxTokens, stream: false);

        _logger.LogInformation("Отправка сообщения в GPT API (модель: {Model})", request.Model);

        var response = await _chatClient.SendChatCompletionAsync(request, cancellationToken);

        // Обновляем текущие лимиты из ответа
        if (response.Usage != null)
        {
            _logger.LogDebug(
                "Использовано токенов - prompt: {PromptTokens}, completion: {CompletionTokens}, всего: {TotalTokens}",
                response.Usage.PromptTokens,
                response.Usage.CompletionTokens,
                response.Usage.TotalTokens);
        }

        return response;
    }

    /// <summary>
    /// Отправить сообщение для обработки с потоковым ответом
    /// </summary>
    public async IAsyncEnumerable<StreamingChatCompletionChunk> SendMessageStreamAsync(
        IEnumerable<ChatMessage> messages,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var request = BuildRequest(messages, model, temperature, maxTokens, stream: true);

        _logger.LogInformation("Отправка сообщения в GPT API со streaming (модель: {Model})", request.Model);

        await foreach (var chunk in _chatClient.StreamChatCompletionAsync(request, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Получить текущие лимиты на использование API
    /// </summary>
    public RateLimit? GetCurrentRateLimits()
    {
        return _currentRateLimit;
    }

    private ChatCompletionRequest BuildRequest(
        IEnumerable<ChatMessage> messages,
        string? model,
        double? temperature,
        int? maxTokens,
        bool stream)
    {
        return new ChatCompletionRequest
        {
            Model = model ?? _options.DefaultModel,
            Messages = messages.ToArray(),
            Temperature = temperature,
            MaxTokens = maxTokens,
            Stream = stream
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GptService));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogDebug("Освобождение ресурсов GptService");

        if (_chatClient != null)
        {
            await _chatClient.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
