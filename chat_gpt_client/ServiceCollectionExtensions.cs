using GptClient.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace GptClient;

/// <summary>
/// Сервис для обращения к GPT апи
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGptClient(this IServiceCollection services)
    {
        services.AddSingleton<IGptServiceFactory>();
    }
}
