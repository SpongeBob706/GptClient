using System;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace GptClient.Client;

/// <summary>
/// Клиент для анализа изображений (Vision)
/// </summary>
public interface IVisionClient
{
    /// <summary>
    /// Проанализировать изображение (байты) с текстовым промптом
    /// </summary>
    Task<ChatCompletion> AnalyzeImageAsync(
        byte[] imageBytes,
        string prompt,
        string mimeType = "image/png",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проанализировать изображение по URL с текстовым промптом
    /// </summary>
    Task<ChatCompletion> AnalyzeImageFromUrlAsync(
        Uri imageUrl,
        string prompt,
        CancellationToken cancellationToken = default);
}
