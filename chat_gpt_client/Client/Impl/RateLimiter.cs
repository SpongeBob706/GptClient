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
    }

    /// <summary>
    /// Получить разрешение на выполнение операции (асинхронно дождётся, если необходимо)
    /// </summary>
    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_limiter == null)
        {
            throw new ObjectDisposedException(nameof(RateLimiter));
        }

        using var lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
        {
            _logger.LogWarning("Не удалось получить разрешение на выполнение запроса. Очередь переполнена.");
            throw new InvalidOperationException("Очередь rate limiter'а переполнена.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_limiter != null)
        {
            _logger.LogDebug("Освобождение ресурсов RateLimiter");
            _limiter.Dispose();
            _limiter = null;
        }

        GC.SuppressFinalize(this);
        await ValueTask.CompletedTask;
    }
}
