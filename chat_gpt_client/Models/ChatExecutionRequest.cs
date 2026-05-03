using System.Collections.Generic;
using OpenAI.Chat;

namespace GptClient.Models;

/// <summary>
/// Параметры запроса на выполнение Chat Completion через OpenAI SDK
/// </summary>
/// <param name="Messages">
/// Сообщения диалога (system / user / assistant), формирующие контекст запроса
/// </param>
/// <param name="Options">
/// Настройки выполнения запроса (temperature, max tokens и другие параметры генерации)
/// </param>
internal sealed record ChatExecutionRequest(
    IEnumerable<ChatMessage> Messages,
    ChatCompletionOptions Options);
