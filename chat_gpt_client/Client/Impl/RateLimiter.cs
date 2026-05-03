using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GptClient.Client.Impl;

/// <summary>
/// Обработчик ограничения скорости (rate limiting) запросов
/// </summary>
internal sealed class RateLimiter : IAsyncDisposable
{
    private readonly ILogger _logger;

    private SlidingWindowRateLimiter? _limiter;

    /// <inheritdoc cref="RateLimiter" />
    public RateLimiter(int requestsPerSecond, ILogger logger)
    {
        _logger = logger;

        var options = new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = requestsPerSecond * 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            PermitLimit = requestsPerSecond,
            Window = TimeSpan.FromSeconds(1)
        };

        _limiter = new SlidingWindowRateLimiter(options);

        _logger.LogDebug(
            "RateLimiter initialized. RPS: {RequestsPerSecond}, QueueLimit: {QueueLimit}",
            requestsPerSecond,
            options.QueueLimit);
    }

    /// <summary>
    /// Получить разрешение на выполнение операции (асинхронно дождётся, если необходимо)
    /// </summary>
    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_limiter == null)
        {
            _logger.LogWarning("Attempt to use disposed RateLimiter");
            throw new ObjectDisposedException(nameof(RateLimiter));
        }

        using var lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
        {
            _logger.LogWarning(
                "Rate limit exceeded. Request rejected due to full queue");

            throw new InvalidOperationException("Rate limiter queue is full");
        }

        _logger.LogTrace("Rate limiter permit acquired");
    }

    public async ValueTask DisposeAsync()
    {
        if (_limiter != null)
        {
            _logger.LogDebug("Disposing RateLimiter");

            _limiter.Dispose();
            _limiter = null;
        }

        GC.SuppressFinalize(this);
        await ValueTask.CompletedTask;
    }
}
