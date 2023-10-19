namespace Squidlr;

public struct ContentIdentifier : IEquatable<ContentIdentifier>
{
    public ContentIdentifier(SocialMediaPlatform platform, string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        Platform = platform;
        Url = url;
    }

    public SocialMediaPlatform Platform { get; set; }

    public string Url { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is ContentIdentifier identifier && Equals(identifier);
    }

    public bool Equals(ContentIdentifier other)
    {
        return Url == other.Url;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Url);
    }

    public static bool operator ==(ContentIdentifier left, ContentIdentifier right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContentIdentifier left, ContentIdentifier right)
    {
        return !(left == right);
    }

    public override string? ToString()
    {
        return Url ?? base.ToString();
    }

    public readonly static ContentIdentifier Unknown = new(SocialMediaPlatform.Unknown, string.Empty);
}
