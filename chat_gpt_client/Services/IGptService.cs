using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace GptClient.Services;

/// <summary>
/// Сервис для обращения к GPT API
/// </summary>
public interface IGptService : IAsyncDisposable
{
    /// <summary>
    /// Сгенерировать текст по промпту
    /// </summary>
    /// <param name="prompt">Текстовый запрос</param>
    /// <param name="systemMessage">Системное сообщение (роль, контекст)</param>
    /// <param name="responseFormat">Формат ответа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Сгенерированный текст</returns>
    Task<string> GenerateTextAsync(
        string prompt,
        string? systemMessage = null,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Изменить текст по инструкции
    /// </summary>
    /// <param name="text">Исходный текст для изменения</param>
    /// <param name="instruction">Инструкция по изменению</param>
    /// <param name="responseFormat">Формат ответа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Изменённый текст</returns>
    Task<string> TransformTextAsync(
        string text,
        string instruction,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Продолжить диалог с историей сообщений
    /// </summary>
    /// <param name="messages">История сообщений</param>
    /// <param name="responseFormat">Формат ответа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ ассистента</returns>
    Task<string> ContinueDialogueAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сгенерировать текст потоково
    /// </summary>
    /// <param name="prompt">Текстовый запрос</param>
    /// <param name="systemMessage">Системное сообщение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Поток фрагментов текста</returns>
    IAsyncEnumerable<string> GenerateTextStreamAsync(
        string prompt,
        string? systemMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Изменить изображение по промпту:
    /// проанализировать изображение, затем сгенерировать новое на основе описания и инструкции
    /// </summary>
    /// <param name="imageBytes">Исходное изображение</param>
    /// <param name="editPrompt">Промпт с описанием желаемых изменений</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL сгенерированного изображения</returns>
    Task<string> EditImageAsync(
        byte[] imageBytes,
        string editPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Изменить изображение по URL промпту
    /// </summary>
    /// <param name="imageUrl">URL исходного изображения</param>
    /// <param name="editPrompt">Промпт с описанием желаемых изменений</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL сгенерированного изображения</returns>
    Task<string> EditImageFromUrlAsync(
        Uri imageUrl,
        string editPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проанализировать изображение — получить текстовое описание
    /// </summary>
    /// <param name="imageBytes">Изображение</param>
    /// <param name="analysisPrompt">Что именно нужно проанализировать (по умолчанию — детальное описание)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Текстовый результат анализа</returns>
    Task<string> DescribeImageAsync(
        byte[] imageBytes,
        string? analysisPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сгенерировать изображение по текстовому описанию
    /// </summary>
    /// <param name="prompt">Описание желаемого изображения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL сгенерированного изображения</returns>
    Task<string> GenerateImageAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Изменить текст на основе анализа изображения
    /// </summary>
    /// <param name="text">Исходный текст</param>
    /// <param name="imageBytes">Изображение для контекста</param>
    /// <param name="instruction">Инструкция по изменению текста с учётом изображения</param>
    /// <param name="responseFormat">Формат ответа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Изменённый текст</returns>
    Task<string> TransformTextWithImageContextAsync(
        string text,
        byte[] imageBytes,
        string instruction,
        ChatResponseFormat? responseFormat = null,
        CancellationToken cancellationToken = default);
}
