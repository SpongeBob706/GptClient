using GptClient.Services;

namespace GptClient.Factories;

/// <summary>
/// Фабрика для создания сервиса обращений к GPT апи
/// </summary>
public interface IGptServiceFactory
{
    /// <summary>
    /// Создать сервис
    /// </summary>
    IGptService Create();
}
