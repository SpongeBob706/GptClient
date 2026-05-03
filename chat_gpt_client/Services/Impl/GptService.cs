using System;
using System.Threading.Tasks;
using GptClient.Client;
using Microsoft.Extensions.Logging;

namespace GptClient.Services.Impl;

/// <summary>
/// Реализация сервиса для работы с GPT API
/// </summary>
internal sealed class GptService : IGptService
{
    private readonly IOpenAiClient _openAiClient;
    private readonly ILogger _logger;
    private bool _disposed;

    public GptService(
        IOpenAiClient openAiClient,
        ILogger<GptService> logger)
    {
        _openAiClient = openAiClient;
        _logger = logger;
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

        if (_openAiClient != null)
        {
            await _openAiClient.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
