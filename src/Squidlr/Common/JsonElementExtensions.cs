using System.Text.Json;

namespace Squidlr.Common;

internal static class JsonElementExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string elementName)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrEmpty(elementName);

        if (element.ValueKind == JsonValueKind.Null)
            return null;

        if (element.TryGetProperty(elementName, out var property))
            return property;

        return null;
    }
}