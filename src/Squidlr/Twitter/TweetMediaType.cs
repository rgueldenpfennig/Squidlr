using System.Text.Json.Serialization;

namespace Squidlr.Twitter;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TweetMediaType
{
    Image,

    Video
}
