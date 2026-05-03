using System;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Client.Impl;

namespace GptClient.Core.Impl;

/// <summary>
/// Retry логика для pipeline
/// </summary>
internal sealed class RetryMiddleware : IAiMiddleware
{
    private readonly RetryHandler _retry;

    /// <inheritdoc cref="RetryMiddleware" />
    public RetryMiddleware(RetryHandler retry)
    {
        _retry = retry;
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
