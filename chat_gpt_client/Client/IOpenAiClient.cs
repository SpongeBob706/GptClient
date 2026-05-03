using System;

namespace GptClient.Client;

/// <summary>
/// Единая точка входа для работы с OpenAI (Chat, Vision, Images)
/// </summary>
public interface IOpenAiClient : IAsyncDisposable
{
    /// <summary>
    /// Клиент для текстовых чатов (Chat Completions)
    /// </summary>
    IChatClient Chat { get; }

    /// <summary>
    /// Клиент для анализа изображений (Vision)
    /// </summary>
    IVisionClient Vision { get; }

    /// <summary>
    /// Клиент для генерации изображений
    /// </summary>
    IImageClient Images { get; }
}
