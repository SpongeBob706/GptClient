using System;
using System.Collections.Generic;

namespace GptClient.Models;

/// <summary>
/// Результат выполнения Responses API.
/// </summary>
public sealed class ResponseExecutionResult
{
    public string ResponseId { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public IReadOnlyCollection<byte[]> Images { get; init; } = Array.Empty<byte[]>();
}
