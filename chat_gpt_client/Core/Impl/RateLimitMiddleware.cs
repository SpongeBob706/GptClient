using System;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Client.Impl;
using GptClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GptClient.Core.Impl;

/// <summary>
/// Ограничение скорости запросов
/// </summary>
internal sealed class RateLimitMiddleware : IAiMiddleware
{
    private readonly RateLimiter _rateLimiter;

    /// <inheritdoc cref="RateLimitMiddleware" />
    public RateLimitMiddleware(IOptions<GptClientOptions> options, ILogger<RateLimitMiddleware> logger)
    {
        _rateLimiter = new RateLimiter(options.Value.RequestsPerSecond, logger);
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
