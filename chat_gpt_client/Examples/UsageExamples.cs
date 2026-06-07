using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Exceptions;
using GptClient.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace GptClient.Examples;

/// <summary>
/// Примеры использования GPT клиента
/// </summary>
public static class UsageExamples
{
    private const string ApiKey = "sk-your-api-key-here";
    private const string DefaultModel = "gpt-4";
    private const string DefaultImageModel = "gpt-4";

    /// <summary>
    /// Пример 1: Базовая генерация текста
    /// </summary>
    public static async Task Example1_BasicTextGenerationAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(config => config.AddConsole());

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
            options.HttpTimeoutSeconds = 30;
            options.MaxRetryAttempts = 3;
            options.RequestsPerSecond = 100;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            // Генерация текста с системным сообщением
            var response = await gptService.GenerateTextAsync(
                prompt: "Привет! Как дела?",
                systemMessage: "Ты полезный помощник.");

            Console.WriteLine("Ответ от GPT:");
            Console.WriteLine(response);
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 2: Потоковая генерация текста
    /// </summary>
    public static async Task Example2_StreamingTextGenerationAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            Console.WriteLine("Стихотворение:");

            // Потоковая генерация текста
            await foreach (var chunk in gptService.GenerateTextStreamAsync(
                prompt: "Напиши стихотворение про кота"))
            {
                Console.Write(chunk);
            }
            Console.WriteLine();
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 3: Трансформация текста
    /// </summary>
    public static async Task Example3_TextTransformationAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            var originalText = "Я вчера ходил в магазин. Купил хлеб. Потом пошёл домой.";

            Console.WriteLine("Исходный текст:");
            Console.WriteLine(originalText);
            Console.WriteLine();

            // Трансформация текста
            var transformedText = await gptService.TransformTextAsync(
                text: originalText,
                instruction: "Сделай текст более художественным и добавь описания");

            Console.WriteLine("Трансформированный текст:");
            Console.WriteLine(transformedText);
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 4: Продолжение диалога с историей сообщений
    /// </summary>
    public static async Task Example4_DialogueContinuationAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            // Используем типы из OpenAI.Chat
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("Ты полезный ассистент."),
                new UserChatMessage("Расскажи о C#."),
                new AssistantChatMessage("C# — это современный объектно-ориентированный язык программирования..."),
                new UserChatMessage("А чем он отличается от Java?")
            };

            var response = await gptService.ContinueDialogueAsync(messages);

            Console.WriteLine("Ответ ассистента:");
            Console.WriteLine(response);
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 5: Генерация изображения
    /// </summary>
    public static async Task Example5_ImageGenerationAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
            options.DefaultImageModel = DefaultImageModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            Console.WriteLine("Генерация изображения...");

            var imageUrl = await gptService.GenerateImageAsync(
                prompt: "Космический кот, играющий на гитаре на фоне галактики, цифровой арт");

            Console.WriteLine($"Изображение сгенерировано: {imageUrl}");
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 6: Анализ изображения
    /// </summary>
    public static async Task Example6_ImageAnalysisAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
            options.DefaultImageModel = DefaultImageModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            // Загружаем изображение из файла
            var imageBytes = await System.IO.File.ReadAllBytesAsync("cat.jpg");

            Console.WriteLine("Анализ изображения...");

            var description = await gptService.DescribeImageAsync(
                imageBytes: imageBytes,
                analysisPrompt: "Опиши, что изображено на этой картинке, какое настроение она передаёт");

            Console.WriteLine("Описание изображения:");
            Console.WriteLine(description);
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 7: Редактирование изображения
    /// </summary>
    public static async Task Example7_ImageEditingAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
            options.DefaultImageModel = DefaultImageModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            var imageBytes = await System.IO.File.ReadAllBytesAsync("cat.jpg");

            Console.WriteLine("Редактирование изображения...");

            var editedImageUrl = await gptService.EditImageWithAnalyseAsync(
                imageBytes: imageBytes,
                editPrompt: "Сделай кота рыжим и добавь ему солнечные очки");

            Console.WriteLine($"Отредактированное изображение: {editedImageUrl}");
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 8: Трансформация текста с контекстом изображения
    /// </summary>
    public static async Task Example8_TextTransformationWithImageAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = DefaultModel;
            options.DefaultImageModel = DefaultImageModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            var imageBytes = await System.IO.File.ReadAllBytesAsync("product.jpg");
            var productDescription = "Этот товар отлично подходит для повседневного использования.";

            Console.WriteLine("Исходное описание:");
            Console.WriteLine(productDescription);
            Console.WriteLine();

            var enhancedDescription = await gptService.TransformTextWithImageContextAsync(
                text: productDescription,
                imageBytes: imageBytes,
                instruction: "Улучши описание товара, основываясь на том, что видно на изображении");

            Console.WriteLine("Улучшенное описание:");
            Console.WriteLine(enhancedDescription);
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 9: С отменой через CancellationToken
    /// </summary>
    public static async Task Example9_WithCancellationAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = ApiKey;
            options.DefaultModel = "gpt-3.5-turbo";
            options.DefaultImageModel = DefaultImageModel;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            using var cts = new CancellationTokenSource();

            // Отмена через 10 секунд
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            try
            {
                var response = await gptService.GenerateTextAsync(
                    prompt: "Напиши очень длинный рассказ о путешествиях во времени",
                    cancellationToken: cts.Token);

                Console.WriteLine(response);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Операция была отменена по таймауту");
            }
        }
        finally
        {
            await gptService.DisposeAsync();
        }
    }

    /// <summary>
    /// Пример 10: Обработка ошибок
    /// </summary>
    public static async Task Example10_ErrorHandlingAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGptClient(options =>
        {
            options.ApiKey = "sk-invalid-key";  // Неверный ключ для демонстрации
            options.DefaultModel = DefaultModel;
            options.MaxRetryAttempts = 2;
        });

        var provider = services.BuildServiceProvider();
        var gptService = provider.GetRequiredService<IGptService>();

        try
        {
            var response = await gptService.GenerateTextAsync(
                prompt: "Привет",
                systemMessage: "Ты полезный ассистент.");

            Console.WriteLine(response);
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

// Запуск примеров (раскомментируйте нужный):
// await UsageExamples.Example1_BasicTextGenerationAsync();
// await UsageExamples.Example2_StreamingTextGenerationAsync();
// await UsageExamples.Example3_TextTransformationAsync();
// await UsageExamples.Example4_DialogueContinuationAsync();
// await UsageExamples.Example5_ImageGenerationAsync();
// await UsageExamples.Example6_ImageAnalysisAsync();
// await UsageExamples.Example7_ImageEditingAsync();
// await UsageExamples.Example8_TextTransformationWithImageAsync();
// await UsageExamples.Example9_WithCancellationAsync();
// await UsageExamples.Example10_ErrorHandlingAsync();
