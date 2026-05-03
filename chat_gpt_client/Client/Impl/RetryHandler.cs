using System;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Exceptions;
using Microsoft.Extensions.Logging;

namespace GptClient.Client.Impl;

/// <summary>
/// Обработчик повторных попыток (retry) с использованием экспоненциальной задержки (exponential backoff)
/// Применяется при временных ошибках (например, сетевых или HTTP 5xx),
/// чтобы не перегружать систему мгновенными повторными запросами
/// </summary>
internal sealed class RetryHandler
{
    private readonly int _maxRetryAttempts;
    private readonly int _initialRetryDelayMs;

    private readonly int _maxRetryDelayMs;
    private readonly double _backoffMultiplier;

    private readonly ILogger _logger;

    /// <inheritdoc cref="RetryHandler" />
    public RetryHandler(
        int maxRetryAttempts,
        int initialRetryDelayMs,
        int maxRetryDelayMs,
        double backoffMultiplier,
        ILogger logger)
    {
        _maxRetryAttempts = maxRetryAttempts;
        _initialRetryDelayMs = initialRetryDelayMs;
        _maxRetryDelayMs = maxRetryDelayMs;
        _backoffMultiplier = backoffMultiplier;
        _logger = logger;
    }

    /// <summary>
    /// Выполнить операцию с ретраями
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken,
        string operationName)
    {
        var attempt = 0;
        var delayMs = _initialRetryDelayMs;

        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                attempt++;

                _logger.LogDebug(
                    "Attempt {Attempt}/{MaxAttempts} for operation '{Operation}'",
                    attempt,
                    _maxRetryAttempts,
                    operationName);

                return await operation(cancellationToken);
            }
            catch (TemporarilyException ex) when (attempt < _maxRetryAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Operation '{Operation}' failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms",
                    operationName,
                    attempt,
                    _maxRetryAttempts,
                    delayMs);

                await Task.Delay(delayMs);

                delayMs = (int)Math.Min(delayMs * _backoffMultiplier, _maxRetryDelayMs);
            }
            catch (UnauthorizedGptException)
            {
                _logger.LogError(
                    "Authorization error while executing operation '{Operation}'",
                    operationName);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while executing operation '{Operation}'",
                    operationName);

                throw;
            }

            if (attempt >= _maxRetryAttempts)
            {
                throw new TemporarilyException(
                    $"Operation '{operationName}' failed after {_maxRetryAttempts} attempts");
            }
        }
    }
}
