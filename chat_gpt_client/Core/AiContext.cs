using System.Collections.Generic;

namespace GptClient.Core;

/// <summary>
/// Контекст выполнения AI запроса
/// </summary>
public sealed class AiContext
{
    /// <summary>
    /// Имя операции
    /// </summary>
    public string OperationName { get; set; } = default!;

    /// <summary>
    /// Данные запроса
    /// </summary>
    public object? Request { get; set; }

    /// <summary>
    /// Метаданные
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();
}
