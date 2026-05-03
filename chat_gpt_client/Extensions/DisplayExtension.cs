using System.Net;

namespace GptClient.Extensions;

/// <summary>
/// Методы расширения для отображения объектов
/// </summary>
public static class DisplayExtension
{
    public static string Display(this HttpStatusCode code) => $"responseStatusCode:({(int)code}){code}";

    private static string DisplayValue<T>(this T value) => $"{(value == null ? "null" : $"'{value}'")}";
}
