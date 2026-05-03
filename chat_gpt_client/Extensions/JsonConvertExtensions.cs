using System;
using System.Text.Json;

namespace GptClient.Extensions;

/// <summary>
/// Методы расширения для Json конвертеров
/// </summary>
public static class JsonConvertExtensions
{
    /// <summary>
    /// Десериализовать строку <paramref name="json"/> в объект <typeparamref name="TModel"/>. Бросает исключение, если десериализация вернёт null
    /// </summary>
    public static TModel DeserializeObjectStrict<TModel>(string json, JsonSerializerOptions? options = null)
    {
        var result = JsonSerializer.Deserialize<TModel>(json, options);
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result), $"Json модель не соответствует десериализуемому типу '{typeof(TModel)}'");
        }

        return result;
    }

    /// <summary>
    /// Получить JSON из объекта
    /// </summary>
    public static string ToJsonString<T>(this T value, JsonSerializerOptions? options = null)
    {
        var str = JsonSerializer.Serialize(value, options);

        return str;
    }
}
