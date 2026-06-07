namespace GptClient.Models;

/// <summary>
/// Изображение для отправки в Responses API.
/// </summary>
public sealed class ResponseImage
{
    public required byte[] Data { get; init; }

    public string MimeType { get; init; } = "image/png";
}
