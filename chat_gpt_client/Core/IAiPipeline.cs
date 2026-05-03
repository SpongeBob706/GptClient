using System;
using System.Threading;
using System.Threading.Tasks;

namespace GptClient.Core;

/// <summary>
/// Конвейер обработки запросов к AI (middleware chain)
/// </summary>
public interface IAiPipeline
{
    /// <summary>
    /// Выполнить операцию через pipeline
    /// </summary>
    Task<T> ExecuteAsync<T>(
        AiContext context,
        Func<AiContext, CancellationToken, Task<T>> terminal,
        CancellationToken cancellationToken);
}
