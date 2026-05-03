using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
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

    /// <inheritdoc cref="ClientBase" />
    protected ClientBase(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
        if ((int)response.StatusCode >= 500)
        {
            var exceptionMessage = $"Не удалось выполнить запрос {response.StatusCode.Display()} по url: '{request.RequestUri}'";

            // показывает, что сервис временно недоступен
            throw new TemporarilyException(exceptionMessage);
        }

        if (!response.IsSuccessStatusCode)
        {
            var encodedContent = Encode(responseContentStr);
            var truncatedResponseContentStr = Truncate(encodedContent);

            var exceptionMessage = $"Не удалось выполнить запрос {response.StatusCode.Display()} по url: '{request.RequestUri}' content: '{truncatedResponseContentStr}'";

            throw new Exception(exceptionMessage);
        }

        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            _logger.LogTrace("Тип контента {Type}. Десериализация в объект {Name}", "application/json", typeof(T));
            return JsonConvertExtensions.DeserializeObjectStrict<T>(responseContentStr);
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
            var response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogTrace("Получен статус код {Code}, вызов метода обновления токена авторизации", HttpStatusCode.Unauthorized.Display());

                var responseContentStr = await response.Content.ReadAsStringAsync(cancellationToken);
                var encodedContent = Encode(responseContentStr);
                var truncatedResponseContentStr = Truncate(encodedContent);

                throw new UnauthorizedGptException(truncatedResponseContentStr ?? "");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить запрос в GPT API");

            throw new TemporarilyException("Не удалось отправить пакет в GPT API", ex);
        }
    }

    private static string Encode(string responseContentStr) => HttpUtility.HtmlEncode(responseContentStr);

    private static string? Truncate(string responseContentStr) => responseContentStr.Truncate(10_000, "...");
}
