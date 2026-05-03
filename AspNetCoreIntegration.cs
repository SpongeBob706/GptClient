using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GptClient;
using GptClient.Models;
using GptClient.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GptClient.Integration;

/// <summary>
/// Пример интеграции GPT клиента в ASP.NET Core приложение
/// </summary>
public static class AspNetCoreIntegration
{
    /// <summary>
    /// Регистрация GPT клиента в Program.cs
    /// </summary>
    public static IServiceCollection AddGptClientService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Метод 1: Использование конфигурации из appsettings.json
        services.AddGptClient(configuration, "GptClient");

        return services;
    }

    /// <summary>
    /// Альтернативный способ - регистрация с явной конфигурацией
    /// </summary>
    public static IServiceCollection AddGptClientServiceManual(
        this IServiceCollection services,
        string apiKey,
        string baseUrl,
        string defaultModel)
    {
        services.AddGptClient(options =>
        {
            options.ApiKey = apiKey;
            options.BaseUrl = baseUrl;
            options.DefaultModel = defaultModel;
        });

        return services;
    }
}

/// <summary>
/// Пример API контроллера для работы с GPT
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IGptService _gptService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IGptService gptService, ILogger<ChatController> logger)
    {
        _gptService = gptService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/chat?message=Привет
    /// Простой запрос к GPT
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetResponse([FromQuery] string message)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = message }
            };

            var response = await _gptService.SendMessageAsync(messages);

            return Ok(new
            {
                success = true,
                message = response.Choices[0].Message.Content,
                usage = response.Usage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке запроса");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/chat/stream
    /// Streaming ответ от GPT
    /// </summary>
    [HttpPost("stream")]
    public async IAsyncEnumerable<string> StreamResponse([FromBody] ChatRequest request)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = request.Message }
            };

            await foreach (var chunk in _gptService.SendMessageStreamAsync(messages))
            {
                if (chunk.Choices[0].Delta?.Content != null)
                {
                    yield return chunk.Choices[0].Delta.Content;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при потоковой обработке запроса");
            yield return $"ERROR: {ex.Message}";
        }
    }

    /// <summary>
    /// POST /api/chat/conversation
    /// Диалог с сохранением контекста
    /// </summary>
    [HttpPost("conversation")]
    public async Task<IActionResult> Conversation([FromBody] ConversationRequest request)
    {
        try
        {
            var response = await _gptService.SendMessageAsync(
                request.Messages,
                temperature: request.Temperature ?? 0.7,
                maxTokens: request.MaxTokens
            );

            return Ok(new
            {
                success = true,
                message = response.Choices[0].Message.Content,
                usage = response.Usage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке диалога");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// Модель запроса для простого сообщения
/// </summary>
public sealed class ChatRequest
{
    public required string Message { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// Модель запроса для диалога
/// </summary>
public sealed class ConversationRequest
{
    public required ChatMessage[] Messages { get; set; }
    public string? Model { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}

/// <summary>
/// Пример Program.cs
/// </summary>
/*
var builder = WebApplicationBuilder.CreateBuilder(args);

// Регистрируем GPT клиент
builder.Services.AddGptClientService(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseRouting();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();
*/
