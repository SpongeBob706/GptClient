using GptClient.Client;
using GptClient.Services;
using GptClient.Services.Impl;
using Microsoft.Extensions.Logging;

namespace GptClient.Factories.Impl;

/// <summary>
/// Реализация фабрики для создания сервиса GPT API
/// </summary>
internal sealed class GptServiceFactory : IGptServiceFactory
{
    private readonly IOpenAiClient _openAiClient;
    private readonly ILoggerFactory _loggerFactory;

    /// <inheritdoc cref="GptServiceFactory" />
    public GptServiceFactory(
        IOpenAiClient openAiClient,
        ILoggerFactory loggerFactory)
    {
        _openAiClient = openAiClient;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Создать сервис обращений к GPT API
    /// </summary>
    public IGptService Create()
    {
        var logger = _loggerFactory.CreateLogger<GptService>();

        return new GptService(_openAiClient, logger);
    }
}
