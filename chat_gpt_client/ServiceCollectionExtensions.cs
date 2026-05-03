using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using GptClient.Client;
using GptClient.Core;
using GptClient.Core.Impl;
using GptClient.Factories;
using GptClient.Factories.Impl;
using GptClient.Models;
using GptClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GptClient;

/// <summary>
/// Расширение для регистрации GPT клиента в DI контейнере
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавить GPT клиент в DI контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="configureOptions">Конфигурация опций (API ключ, URL и т.д.)</param>
    /// <returns>Коллекция сервисов для chain операций</returns>
    public static IServiceCollection AddGptClient(
        this IServiceCollection services,
        Action<GptClientOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        // Регистрируем опции
        services.Configure(configureOptions);

        AddHttpClient(services);

        // регистрируем клиенты и пайплайн обрабработки запросов до openAi
        AddAiClient(services);

        // Регистрируем фабрику сервиса
        services.AddSingleton<IGptServiceFactory, GptServiceFactory>();

        // Регистрируем сам сервис как singleton через фабрику
        services.AddSingleton<IGptService>(provider =>
        {
            var factory = provider.GetRequiredService<IGptServiceFactory>();
            return factory.Create();
        });

        return services;
    }

    /// <summary>
    /// Добавить GPT клиент в DI контейнер с конфигурацией из IConfiguration
    /// </summary>
    public static IServiceCollection AddGptClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "GptClient")
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var section = configuration.GetSection(sectionName);
        services.Configure<GptClientOptions>(section);

        AddHttpClient(services);

        // регистрируем клиенты и пайплайн обрабработки запросов до openAi
        AddAiClient(services);

        // Регистрируем фабрику сервиса
        services.AddSingleton<IGptServiceFactory, GptServiceFactory>();

        // Регистрируем сам сервис как singleton через фабрику
        services.AddSingleton<IGptService>(provider =>
        {
            var factory = provider.GetRequiredService<IGptServiceFactory>();
            return factory.Create();
        });

        return services;
    }

    private static void AddHttpClient(IServiceCollection services)
    {
        // Регистрируем HTTP factory если её ещё нет
        if (!services.Any(x => x.ServiceType == typeof(IHttpClientFactory)))
        {
            services.AddHttpClient();
        }
    }

    private static void AddAiClient(IServiceCollection services)
    {
        services.AddSingleton<IOpenAiClient, OpenAiClient>();

        services.AddSingleton<IAiPipeline, AiPipeline>();

        // порядок регистрации важен
        services.AddSingleton<IAiMiddleware, LoggingMiddleware>();
        services.AddSingleton<IAiMiddleware, RateLimitMiddleware>();
        services.AddSingleton<IAiMiddleware, RetryMiddleware>();
    }
}
