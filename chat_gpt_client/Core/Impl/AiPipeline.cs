using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GptClient.Core.Impl;

/// <summary>
/// Исполнитель middleware pipeline
/// </summary>
internal sealed class AiPipeline : IAiPipeline
{
    private readonly IReadOnlyList<IAiMiddleware> _middlewares;

    /// <summary>
    /// Инициализация pipeline
    /// </summary>
    public AiPipeline(IEnumerable<IAiMiddleware> middlewares)
    {
        _middlewares = middlewares.ToList();
    }

    /// <summary>
    /// Выполнить цепочку middleware
    /// </summary>
    public Task<T> ExecuteAsync<T>(
        AiContext context,
        Func<AiContext, CancellationToken, Task<T>> terminal,
        CancellationToken cancellationToken)
    {
        var next = terminal;

        foreach (var middleware in _middlewares.Reverse())
        {
            var currentNext = next;

            next = (ctx, ct) =>
                middleware.InvokeAsync(ctx, currentNext, ct);
        }

        return next(context, cancellationToken);
    }
}
