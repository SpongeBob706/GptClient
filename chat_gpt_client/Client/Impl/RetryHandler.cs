using System;
using System.Threading.Tasks;
using GptClient.Exceptions;
using Microsoft.Extensions.Logging;

namespace GptClient.Client.Impl;

/// <summary>
/// Обработчик повторных попыток (retry) с использованием экспоненциальной задержки (exponential backoff)
/// Применяется при временных ошибках (например, сетевых или HTTP 5xx)
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
    /// <param name="maxRetryAttempts">
    /// Максимальное количество попыток выполнения операции
    /// </param>
    /// <param name="initialRetryDelayMs">
    /// Начальная задержка перед первой повторной попыткой (в миллисекундах)
    /// </param>
    /// <param name="maxRetryDelayMs">
    /// Максимальное ограничение задержки между попытками
    /// </param>
    /// <param name="backoffMultiplier">
    /// Коэффициент увеличения задержки
    /// Например: 2.0 => 100ms, 200ms, 400ms, 800ms и т.д.
    /// </param>
    /// <param name="logger">
    /// </param>
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
        Func<Task<T>> operation,
        string operationName)
    {
        var attempt = 0;
        var delayMs = _initialRetryDelayMs;

        while (true)
        {
            try
            {
                attempt++;
                _logger.LogDebug(
                    "Попытка {Attempt}/{MaxAttempts} для операции '{Operation}'",
                    attempt,
                    _maxRetryAttempts,
                    operationName);

                return await operation();
            }
            catch (TemporarilyException ex) when (attempt < _maxRetryAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Операция '{Operation}' не удалась на попытке {Attempt}/{MaxAttempts}. Повтор через {DelayMs}ms",
                    operationName,
                    attempt,
                    _maxRetryAttempts,
                    delayMs);

                await Task.Delay(delayMs);

                delayMs = (int)Math.Min(delayMs * _backoffMultiplier, _maxRetryDelayMs);
            }
            catch (UnauthorizedGptException)
            {
                _logger.LogError("Ошибка авторизации при выполнении операции '{Operation}'", operationName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при выполнении операции '{Operation}'", operationName);
                throw;
            }

            if (attempt >= _maxRetryAttempts)
            {
                throw new TemporarilyException(
                    $"Операция '{operationName}' не удалась после {_maxRetryAttempts} попыток");
            }
        }
    }
}
