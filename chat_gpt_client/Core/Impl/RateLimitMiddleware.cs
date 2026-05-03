using System;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Client.Impl;

namespace GptClient.Core.Impl;

/// <summary>
/// Ограничение скорости запросов
/// </summary>
internal sealed class RateLimitMiddleware : IAiMiddleware
{
    private readonly RateLimiter _rateLimiter;

    /// <inheritdoc cref="RateLimitMiddleware" />
    public RateLimitMiddleware(RateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    /// <inheritdoc />
    public async Task<T> InvokeAsync<T>(
        AiContext context,
        Func<AiContext, CancellationToken, Task<T>> next,
        CancellationToken cancellationToken)
    {
        await _rateLimiter.AcquireAsync(cancellationToken);

        return await next(context, cancellationToken);
    }
}
