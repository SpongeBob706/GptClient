using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GptClient.Core;
using GptClient.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using ResponseResult = OpenAI.Responses.ResponseResult;

namespace GptClient.Client.Impl;

/// <summary>
/// Клиент для работы с Responses API с поддержкой истории
/// </summary>
internal sealed class ResponseImageClient : IResponseImageClient
{
    private readonly ResponsesClient _client;
    private readonly IAiPipeline _pipeline;
    private readonly ILogger _logger;
    private readonly string _gptModel;
    private readonly string _gptImageModel;

    private readonly ConcurrentDictionary<string, ResponseSession>
        _sessions = new();

    public ResponseImageClient(
        string gptModel,
        string gptImageModel,
        ResponsesClient client,
        IAiPipeline pipeline,
        ILogger logger)
    {
        _gptModel = gptModel;
        _gptImageModel = gptImageModel;

        _client = client;
        _pipeline = pipeline;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResponseExecutionResult> ExecuteAsync(
        ResponseRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = GetOrCreateSession(request.SessionId);

        return await _pipeline.ExecuteAsync(
            new AiContext
            {
                OperationName = "ResponseExecute",
                Request = request.Prompt
            },
            async (_, ct) =>
            {
                var options = new CreateResponseOptions
                {
                    Model = _gptModel,
                    PreviousResponseId = request.ContinueConversation
                        ? session.LastResponseId
                        : null,

                    // ДОБАВЛЯЕМ ИНСТРУМЕНТ ГЕНЕРАЦИИ
                    Tools =
                    {
                        ResponseTool.CreateImageGenerationTool(
                            _gptImageModel,
                            quality: request.Quality,
                            size: request.Size),
                    }
                };

                // Добавляем входные данные
                foreach (var (image, prompt) in request.Images)
                {
                    var content = new List<ResponseContentPart>();

                    if (image != null)
                    {
                        content.Add(ResponseContentPart.CreateInputImagePart(
                            BinaryData.FromBytes(image.Data, image.MimeType),
                            null));
                    }

                    if (!string.IsNullOrWhiteSpace(prompt))
                    {
                        content.Add(ResponseContentPart.CreateInputTextPart(prompt));
                    }

                    options.InputItems.Add(ResponseItem.CreateUserMessageItem(content));
                }

                var result = await _client.CreateResponseAsync(options, ct);
                var response = result.Value;

                session.LastResponseId = response.Id;

                return ParseResponse(response);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CancelResponseAsync(string responseId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cancelling response: {ResponseId}", responseId);

        return await _pipeline.ExecuteAsync<bool>(
            new AiContext
            {
                OperationName = "CancelResponse",
                Request = responseId
            },
            async (_, ct) =>
            {
                var result = await _client.CancelResponseAsync(responseId, ct);
                return result != null;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteResponseAsync(string responseId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting response: {ResponseId}", responseId);

        return await _pipeline.ExecuteAsync<bool>(
            new AiContext
            {
                OperationName = "DeleteResponse",
                Request = responseId
            },
            async (_, ct) =>
            {
                var result = await _client.DeleteResponseAsync(responseId, ct);
                return result != null && result.Value.Deleted;
            },
            cancellationToken);
    }

    private ResponseExecutionResult ParseResponse(
        ResponseResult response)
    {
        var text = new StringBuilder();

        var images = new List<byte[]>();

        foreach (var item in response.OutputItems)
        {
            switch (item)
            {
                case MessageResponseItem message:
                    foreach (var content in message.Content)
                    {
                        if (!string.IsNullOrEmpty(content.Text))
                        {
                            text.AppendLine(content.Text);
                        }
                    }

                    break;

                case ImageGenerationCallResponseItem message:
                    if (message.ImageResultBytes is not null)
                    {
                        images.Add(message.ImageResultBytes.ToArray());
                    }

                    break;
            }
        }

        return new ResponseExecutionResult
        {
            ResponseId = response.Id,
            Text = text.ToString(),
            Images = images,
        };
    }

    private ResponseSession GetOrCreateSession(
        string sessionId)
    {
        return _sessions.GetOrAdd(
            sessionId,
            id => new ResponseSession
            {
                SessionId = id
            });
    }
}
