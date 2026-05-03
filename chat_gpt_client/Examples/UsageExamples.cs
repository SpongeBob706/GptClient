using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Exceptions;
using GptClient.Models;
using GptClient.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GptClient.Examples;

/// <summary>
/// Примеры использования GPT клиента
/// </summary>
public static class UsageExamples
{
    /// <summary>
    /// Пример 1: Базовое использование с конфигурацией через лямбду
    /// </summary>
    public static async Task Example1_BasicUsageAsync()
    {
        // Создаём DI контейнер
        var services = new ServiceCollection();
        services.AddLogging(config =>
        {
            config.AddConsole();
        });

        // Регистрируем GPT клиент с конфигурацией
        services.AddGptClient(options =>
        {
            options.ApiKey = "sk-your-api-key-here";
            options.BaseUrl = "https://api.openai.com/v1";
            options.DefaultModel = "gpt-4";
            options.HttpTimeoutSeconds = 30;
            options.MaxRetryAttempts = 3;
            options.RequestsPerSecond = 100;
        });

        var provider = services.BuildServiceProvider();

        // Получаем сервис из контейнера
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            // Создаём сообщения
            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = "Ты полезный помощник." },
                new() { Role = "user", Content = "Привет! Как дела?" }
            };

            // Отправляем запрос без streaming
            var response = await gptService.SendMessageAsync(messages);

            // Выводим ответ
            if (response.Choices.Length > 0)
            {
                Console.WriteLine("Ответ от GPT:");
                Console.WriteLine(response.Choices[0].Message.Content);
            }
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 2: Использование streaming ответа
    /// </summary>
    public static async Task Example2_StreamingAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = "sk-your-api-key-here";
            options.BaseUrl = "https://api.openai.com/v1";
            options.DefaultModel = "gpt-4";
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Напиши стихотворение про кота" }
            };

            // Отправляем streaming запрос
            Console.WriteLine("Стихотворение:");
            await foreach (var chunk in gptService.SendMessageStreamAsync(messages))
            {
                if (chunk.Choices.Length > 0 && chunk.Choices[0].Delta?.Content != null)
                {
                    Console.Write(chunk.Choices[0].Delta.Content);
                }
            }
            Console.WriteLine();
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 3: С отменой через CancellationToken
    /// </summary>
    public static async Task Example3_WithCancellationAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = "sk-your-api-key-here";
            options.BaseUrl = "https://api.openai.com/v1";
            options.DefaultModel = "gpt-3.5-turbo";
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            using var cts = new CancellationTokenSource();

            // Отмена через 10 секунд
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Напиши очень длинный рассказ" }
            };

            try
            {
                var response = await gptService.SendMessageAsync(messages, cancellationToken: cts.Token);
                Console.WriteLine(response.Choices[0].Message.Content);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Операция была отменена");
            }
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 4: С параметрами запроса
    /// </summary>
    public static async Task Example4_WithParametersAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = "sk-your-api-key-here";
            options.BaseUrl = "https://api.openai.com/v1";
            options.DefaultModel = "gpt-4";
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Один" }
            };

            // Отправляем с пользовательскими параметрами
            var response = await gptService.SendMessageAsync(
                messages,
                temperature: 0.7,           // Креативность (0-2)
                maxTokens: 100              // Максимум токенов в ответе
            );

            Console.WriteLine("Ответ:");
            Console.WriteLine(response.Choices[0].Message.Content);

            if (response.Usage != null)
            {
                Console.WriteLine($"Использовано токенов: {response.Usage.TotalTokens}");
            }
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 5: Обработка ошибок
    /// </summary>
    public static async Task Example5_ErrorHandlingAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = "sk-invalid-key";  // Неверный ключ
            options.BaseUrl = "https://api.openai.com/v1";
            options.DefaultModel = "gpt-4";
            options.MaxRetryAttempts = 2;       // Небольшое количество ретраев
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Привет" }
            };

            var response = await gptService.SendMessageAsync(messages);
            Console.WriteLine(response.Choices[0].Message.Content);
        }
        catch (UnauthorizedGptException ex)
        {
            Console.WriteLine($"Ошибка авторизации: {ex.Message}");
        }
        catch (TemporarilyException ex)
        {
            Console.WriteLine($"Временная ошибка сервера: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }
}

// Запуск примеров
// await UsageExamples.Example1_BasicUsageAsync();
// await UsageExamples.Example2_StreamingAsync();
// await UsageExamples.Example3_WithCancellationAsync();
// await UsageExamples.Example4_WithParametersAsync();
// await UsageExamples.Example5_ErrorHandlingAsync();
