using System.Collections.ObjectModel;

namespace Squidlr;

public sealed class Video
{
    public Uri? DisplayUrl { get; set; }

    public TimeSpan? Duration { get; set; }

    public int? Views { get; set; }

    public bool? Monetizable { get; set; }

    public VideoSourceCollection VideoSources { get; set; } = [];

    public void AddVideoSource(VideoSource videoSource)
    {
        ArgumentNullException.ThrowIfNull(videoSource);
        VideoSources.Add(videoSource);
    }
}

public sealed class VideoCollection : Collection<Video>
{
}
