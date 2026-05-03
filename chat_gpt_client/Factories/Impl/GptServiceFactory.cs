using System;
using System.Net.Http;
using GptClient.Core;
using GptClient.Models;
using GptClient.Services;
using GptClient.Services.Impl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GptClient.Factories.Impl;

/// <summary>
/// Реализация фабрики для создания сервиса GPT API
/// </summary>
internal sealed class GptServiceFactory : IGptServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<GptClientOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public GptServiceFactory(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IOptions<GptClientOptions> options,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Создать сервис обращений к GPT API
    /// </summary>
    public IGptService Create()
    {
        var logger = _loggerFactory.CreateLogger<RetryHandler>();

        // Создаём сервис
        return new GptService(gptClient, _options, serviceLogger);
    }
}
