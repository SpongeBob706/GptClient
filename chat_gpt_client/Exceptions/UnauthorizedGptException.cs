using System;

namespace GptClient.Exceptions;

/// <summary>
/// Ошибка авторизации в Gpt апи
/// </summary>
public class UnauthorizedGptException : Exception
{
    /// <inheritdoc cref="UnauthorizedGptException" />
    public UnauthorizedGptException(string message) : base(message) { }

    /// <inheritdoc cref="UnauthorizedGptException" />
    public UnauthorizedGptException(string message, Exception inner) : base(message, inner) { }
}
