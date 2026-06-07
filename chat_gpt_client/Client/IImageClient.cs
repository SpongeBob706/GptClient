using System.Threading;
using System.Threading.Tasks;
using OpenAI.Images;

namespace GptClient.Client;

/// <summary>
/// Клиент для генерации изображений
/// </summary>
public interface IImageClient
{
    /// <summary>
    /// Сгенерировать изображение по текстовому описанию
    /// </summary>
    Task<byte[]> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отредактировать изображение по текстовому описанию
    /// </summary>
    Task<byte[]> EditAsync(
        byte[] image,
        string prompt,
        GeneratedImageQuality? quality = null,
        GeneratedImageSize? size = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отредактировать изображение по текстовому описанию с маской
    /// </summary>
    Task<byte[]> EditAsync(
        byte[] image,
        string prompt,
        byte[] mask,
        GeneratedImageQuality? quality = null,
        GeneratedImageSize? size = null,
        CancellationToken cancellationToken = default);
}
