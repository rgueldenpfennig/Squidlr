using System.Text.Json.Serialization;

namespace Squidlr;

public abstract class Content
{
    public string SourceUrl { get; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SocialMediaPlatform Platform { get; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? Username { get; set; }

    public string? FullText { get; set; }

    public int FavoriteCount { get; set; }

    public int ReplyCount { get; set; }

    public VideoCollection Videos { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();

    protected Content(string sourceUrl, SocialMediaPlatform platform)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceUrl);
        SourceUrl = sourceUrl;
        Platform = platform;
    }

    public void AddVideo(Video video)
    {
        ArgumentNullException.ThrowIfNull(video);
        Videos.Add(video);
    }

    public virtual string GetSafeVideoFileName(VideoSource video)
    {
        if (!string.IsNullOrEmpty(Username))
        {
            return $"{Platform.GetPlatformName()}-{Username}-{Path.GetFileName(video.Url.AbsolutePath)}";
        }

        return $"{Platform.GetPlatformName()}-{Path.GetFileName(video.Url.AbsolutePath)}";
    }
}
