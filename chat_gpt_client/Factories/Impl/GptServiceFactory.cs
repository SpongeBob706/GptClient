using System;
using System.Net.Http;
using GptClient.Client.Impl;
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
        var gptClientLogger = _loggerFactory.CreateLogger<Client.Impl.GptClient>();
        var serviceLogger = _loggerFactory.CreateLogger<GptService>();

        // Создаём retry handler
        var retryHandler = new RetryHandler(
            _options.Value.MaxRetryAttempts,
            _options.Value.InitialRetryDelayMs,
            _options.Value.MaxRetryDelayMs,
            _options.Value.RetryBackoffMultiplier,
            logger);

        // Создаём rate limiter
        var rateLimiter = new RateLimiter(_options.Value.RequestsPerSecond, logger);

        // Создаём GPT клиент
        var gptClient = new Client.Impl.GptClient(
            _httpClientFactory,
            _options.Value.BaseUrl,
            _options.Value.ApiKey,
            gptClientLogger,
            retryHandler,
            rateLimiter);

        // Создаём сервис
        return new GptService(gptClient, _options, serviceLogger);
    }
}
