namespace Squidlr;

public readonly struct VideoSize : IEquatable<VideoSize>
{
    public readonly int Height { get; init; }

    public readonly int Width { get; init; }

    public static VideoSize Empty => new() { Height = 0, Width = 0 };

    public VideoSize(int height, int width)
    {
        Height = height;
        Width = width;
    }

    public override bool Equals(object? obj)
    {
        return obj is VideoSize size && Equals(size);
    }

    public bool Equals(VideoSize other)
    {
        return Height == other.Height && Width == other.Width;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Height, Width);
    }

    public static bool operator ==(VideoSize left, VideoSize right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VideoSize left, VideoSize right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Width}x{Height}";
    }
}
