using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Extensions;
using GptClient.Models;
using Microsoft.Extensions.Logging;

namespace GptClient.Client.Impl;

/// <summary>
/// Клиент для GPT API (OpenAI-совместимый)
/// </summary>
internal sealed class GptClient : ClientBase, IGptClient
{
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly RetryHandler _retryHandler;
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public GptClient(
        IHttpClientFactory httpClientFactory,
        string baseUrl,
        string apiKey,
        ILogger logger,
        RetryHandler retryHandler,
        RateLimiter rateLimiter)
        : base(httpClientFactory, logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _logger = logger;
        _retryHandler = retryHandler;
        _rateLimiter = rateLimiter;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    }

    /// <summary>
    /// Отправить запрос на chat completion без streaming
    /// </summary>
    public async Task<ChatCompletionResponse> SendChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        request.Stream = false;

        return await _retryHandler.ExecuteAsync(
            async () =>
            {
                await _rateLimiter.AcquireAsync(cancellationToken);

                var json = request.ToJsonString(_jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
                {
                    Content = content
                };

                AddAuthHeader(httpRequest);

                _logger.LogDebug("Отправка chat completion запроса");

                return await SendAsync<ChatCompletionResponse>(httpRequest, cancellationToken: cancellationToken);
            },
            "SendChatCompletion");
    }

    /// <summary>
    /// Отправить запрос на chat completion со streaming ответом
    /// </summary>
    public async IAsyncEnumerable<StreamingChatCompletionChunk> StreamChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        request.Stream = true;

        var json = request.ToJsonString(_jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = content
        };

        AddAuthHeader(httpRequest);

        _logger.LogDebug("Отправка streaming chat completion запроса");

        await foreach (var chunk in SendStreamAsync<StreamingChatCompletionChunk>(
            httpRequest,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            if (chunk != null)
            {
                await _rateLimiter.AcquireAsync(cancellationToken);
                yield return chunk;
            }
        }
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GptClient));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogDebug("Освобождение ресурсов GptClient");

        if (_rateLimiter != null)
        {
            await _rateLimiter.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
