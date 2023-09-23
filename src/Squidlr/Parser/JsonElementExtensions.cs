using System.Text.Json;

namespace Squidlr.Parser;

internal static class JsonElementExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string elementName)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrEmpty(elementName);

        if (element.TryGetProperty(elementName, out var property))
            return property;

        return null;
    }
}