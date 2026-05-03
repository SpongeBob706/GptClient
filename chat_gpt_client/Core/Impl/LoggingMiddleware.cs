using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GptClient.Core.Impl;

/// <summary>
/// Логирование запросов pipeline
/// </summary>
internal sealed class LoggingMiddleware : IAiMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    /// <inheritdoc cref="LoggingMiddleware" />
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> InvokeAsync<T>(
        AiContext context,
        Func<AiContext, CancellationToken, Task<T>> next,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Starting operation {Operation}",
            context.OperationName);

        var result = await next(context, cancellationToken);

        _logger.LogDebug(
            "Finished operation {Operation}",
            context.OperationName);

        return result;
    }
}
