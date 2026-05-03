using System;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GptClient.Core.Impl;

/// <summary>
/// Retry логика для pipeline
/// </summary>
internal sealed class RetryMiddleware : IAiMiddleware
{
    private readonly RetryHandler _retry;

    /// <inheritdoc cref="RetryMiddleware" />
    public RetryMiddleware(IOptions<GptClientOptions> options, ILogger<RetryMiddleware> logger)
    {
        var opt = options.Value;

        _retry = new RetryHandler(
            opt.MaxRetryAttempts,
            opt.InitialRetryDelayMs,
            opt.MaxRetryDelayMs,
            opt.RetryBackoffMultiplier,
            logger);
    }

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(
        AiContext context,
        Func<AiContext, CancellationToken, Task<T>> next,
        CancellationToken cancellationToken)
    {
        return _retry.ExecuteAsync(
            ct => next(context, ct),
            cancellationToken,
            context.OperationName);
    }
}
