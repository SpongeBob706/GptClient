using System.Threading;
using System.Threading.Tasks;
using GptClient.Models;

namespace GptClient.Client;

/// <summary>
/// Клиент для работы с Responses API с поддержкой истории
/// </summary>
public interface IResponseImageClient
{
    /// <summary>
    /// Выполнить запрос.
    /// </summary>
    Task<ResponseExecutionResult> ExecuteAsync(
        ResponseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменить response.
    /// </summary>
    Task<bool> CancelResponseAsync(
        string responseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить response.
    /// </summary>
    Task<bool> DeleteResponseAsync(
        string responseId,
        CancellationToken cancellationToken = default);
}
