using System.Text.Json.Serialization;

namespace Squidlr;

public abstract class Content
{
    public string SourceUrl { get; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SocialMediaPlatform Platform { get; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? UserName { get; set; }

    public string? FullText { get; set; }

    public int FavoriteCount { get; set; }

    public int ReplyCount { get; set; }

    protected Content(string sourceUrl, SocialMediaPlatform platform)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceUrl);
        SourceUrl = sourceUrl;
        Platform = platform;
    }
}
