namespace Squidlr;

public struct ContentIdentifier : IEquatable<ContentIdentifier>
{
    public ContentIdentifier(SocialMediaPlatform platform, string id, string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        Platform = platform;
        Id = id;
        Url = url;
    }

    public SocialMediaPlatform Platform { get; set; }

    public string Id { get; set; }

    public string Url { get; set; }

    public override readonly bool Equals(object? obj)
    {
        return obj is ContentIdentifier identifier && Equals(identifier);
    }

    public readonly bool Equals(ContentIdentifier other)
    {
        return Id == other.Id;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public static bool operator ==(ContentIdentifier left, ContentIdentifier right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContentIdentifier left, ContentIdentifier right)
    {
        return !(left == right);
    }

    public override readonly string? ToString()
    {
        if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Url))
        {
            return $"ID: {Id} Url: {Url}";
        }

        return base.ToString();
    }

    public readonly static ContentIdentifier Unknown = new(SocialMediaPlatform.Unknown, string.Empty, string.Empty);
}
