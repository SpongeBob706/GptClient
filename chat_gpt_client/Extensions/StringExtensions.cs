using System;

namespace GptClient.Extensions;

/// <summary>
/// Содержит методы, расширяющие работу со строками
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Обрезать строку для нужной длины
    /// </summary>
    /// <param name="value">Текст для обрезки</param>
    /// <param name="maxLength">Максимальная длина</param>
    /// <param name="clipText">При обрезке текста в конце строки добавляется вот этот текст</param>
    public static string? Truncate(
        this string? value,
        int maxLength,
        string clipText = "")
    {
        if (maxLength - clipText.Length < 0)
        {
            throw new ArgumentException("Максимальная длина текста не может быть меньше 0", nameof(maxLength));
        }

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : $"{value.Substring(0, maxLength - clipText.Length)}{clipText}";
    }
}
