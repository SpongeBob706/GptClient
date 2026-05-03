using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GptClient.Exceptions;
using GptClient.Extensions;
using Microsoft.Extensions.Logging;

namespace GptClient.Client.Impl;

/// <summary>
/// Базовый http-клиент
/// </summary>
internal abstract class ClientBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <inheritdoc cref="ClientBase" />
    protected ClientBase(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// Исполняет отправку запроса и обрабатывает ответ
    /// </summary>
    protected async Task<T> SendAsync<T>(
        HttpRequestMessage request,
        string? httpClientType = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SendImplAsync(request, httpClientType, cancellationToken);
        var responseContentStr = await response.Content.ReadAsStringAsync(cancellationToken);

        ThrowIfErrorStatus(response, responseContentStr);

        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            _logger.LogTrace("Тип контента {Type}. Десериализация в объект {Name}", "application/json", typeof(T));
            return JsonConvertExtensions.DeserializeObjectStrict<T>(responseContentStr, _jsonOptions);
        }

        var converter = TypeDescriptor.GetConverter(typeof(T));
        var fromContent = converter.ConvertFromString(responseContentStr);
        if (fromContent == null)
        {
            _logger.LogTrace("Контент пустой (= null)");
            throw new ArgumentNullException(nameof(fromContent), "Ошибка конвертации");
        }

        return (T)fromContent;
    }

    /// <summary>
    /// Отправить streaming запрос
    /// </summary>
    protected async IAsyncEnumerable<T> SendStreamAsync<T>(
        HttpRequestMessage request,
        string? httpClientType = null,
        CancellationToken cancellationToken = default)
    {
        using var client = string.IsNullOrWhiteSpace(httpClientType)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(httpClientType);

        using var response = await SendImplAsync(request, httpClientType, cancellationToken);

        if ((int)response.StatusCode >= 400)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            ThrowIfErrorStatus(response, errorContent);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Пропускаем префикс "data: "
            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
            {
                var jsonData = line["data: ".Length..];

                // Проверяем на конец потока
                if (jsonData == "[DONE]")
                {
                    _logger.LogDebug("Получен маркер завершения потока");
                    break;
                }

                T chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Ошибка десериализации chunk'а: {Data}", jsonData);
                    continue;
                }

                yield return chunk;
            }
        }
    }

    private async Task<HttpResponseMessage> SendImplAsync(
        HttpRequestMessage request,
        string? httpClientType,
        CancellationToken cancellationToken)
    {
        using var client = string.IsNullOrWhiteSpace(httpClientType)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(httpClientType);

        try
        {
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogTrace("Получен статус код {Code}, вызов метода обновления токена авторизации",
                    HttpStatusCode.Unauthorized.Display());

                var responseContentStr = await response.Content.ReadAsStringAsync(cancellationToken);
                var encodedContent = Encode(responseContentStr);
                var truncatedResponseContentStr = Truncate(encodedContent);

                throw new UnauthorizedGptException(truncatedResponseContentStr ?? "");
            }

            return response;
        }
        catch (UnauthorizedGptException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить запрос в GPT API");
            throw new TemporarilyException("Не удалось отправить запрос в GPT API", ex);
        }
    }

    private void ThrowIfErrorStatus(HttpResponseMessage response, string responseContent)
    {
        if ((int)response.StatusCode >= 500)
        {
            var exceptionMessage =
                $"Не удалось выполнить запрос {response.StatusCode.Display()} по url: '{response.RequestMessage?.RequestUri}'";
            _logger.LogError(exceptionMessage);
            throw new TemporarilyException(exceptionMessage);
        }

        if (!response.IsSuccessStatusCode)
        {
            var encodedContent = Encode(responseContent);
            var truncatedResponseContentStr = Truncate(encodedContent);

            var exceptionMessage =
                $"Не удалось выполнить запрос {response.StatusCode.Display()} по url: '{response.RequestMessage?.RequestUri}' content: '{truncatedResponseContentStr}'";
            _logger.LogError(exceptionMessage);

            throw new Exception(exceptionMessage);
        }
    }

    private static string Encode(string responseContentStr) => HttpUtility.HtmlEncode(responseContentStr);

    private static string? Truncate(string responseContentStr) => responseContentStr.Truncate(10_000, "...");
}
