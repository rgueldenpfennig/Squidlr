using System.Text.Json.Serialization;

namespace Squidlr;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TweetMediaType
{
    Image,

    Video
}
