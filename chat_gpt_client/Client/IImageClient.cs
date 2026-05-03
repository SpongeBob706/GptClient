using System.Threading;
using System.Threading.Tasks;

namespace GptClient.Client;

/// <summary>
/// Клиент для генерации изображений
/// </summary>
public interface IImageClient
{
    /// <summary>
    /// Сгенерировать изображение по текстовому описанию
    /// </summary>
    Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
