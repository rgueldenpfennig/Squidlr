using System.Collections.ObjectModel;

namespace Squidlr;

public sealed class VideoSource
{
    public required Uri Url { get; set; }

    public required int Bitrate { get; set; }

    public long? ContentLength { get; set; }

    public required string ContentType { get; set; }

    public required VideoSize Size { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is VideoSource source &&
               EqualityComparer<Uri>.Default.Equals(Url, source.Url);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Url);
    }

    public override string ToString()
    {
        return Url.ToString();
    }
}

public sealed class VideoSourceCollection : Collection<VideoSource>
{
}
