using System;
using System.Threading;
using System.Threading.Tasks;

namespace GptClient.Core;

/// <summary>
/// Middleware для обработки AI запросов
/// </summary>
public interface IAiMiddleware
{
    /// <summary>
    /// Выполнить шаг pipeline и передать управление дальше
    /// </summary>
    Task<T> InvokeAsync<T>(
        AiContext context,
        Func<AiContext, CancellationToken, Task<T>> next,
        CancellationToken cancellationToken);
}
