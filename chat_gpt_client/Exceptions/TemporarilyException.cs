using System;

namespace GptClient.Exceptions;

/// <summary>
/// Временная ошибка
/// </summary>
public class TemporarilyException : Exception
{
    /// <inheritdoc cref="TemporarilyException" />
    public TemporarilyException(string message) : base(message) { }

    /// <inheritdoc cref="TemporarilyException" />
    public TemporarilyException(string message, Exception inner) : base(message, inner) { }
}
