using System.Text.Json.Serialization;

namespace Squidlr;

public abstract class Content
{
    public string SourceUrl { get; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SocialMediaPlatform Platform { get; }

    protected Content(string sourceUrl, SocialMediaPlatform platform)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceUrl);
        SourceUrl = sourceUrl;
        Platform = platform;
    }
}
