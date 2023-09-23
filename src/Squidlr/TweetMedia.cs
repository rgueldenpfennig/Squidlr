using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Squidlr;

[JsonDerivedType(typeof(TweetMedia), typeDiscriminator: "base")]
[JsonDerivedType(typeof(TweetMediaVideo), typeDiscriminator: "withVideo")]
public class TweetMedia
{
    public required Uri MediaUrl { get; init; }

    public bool? Monetizable { get; set; }

    public int? Views { get; set; }

    public TweetMediaType Type { get; init; }

    public TweetMedia(TweetMediaType type)
    {
        Type = type;
    }
}

public sealed class TweetMediaCollection : Collection<TweetMedia>
{
}