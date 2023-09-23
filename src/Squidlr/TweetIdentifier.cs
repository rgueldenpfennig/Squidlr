namespace Squidlr;

public struct TweetIdentifier : IEquatable<TweetIdentifier>
{
    public TweetIdentifier(string id, string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(url);
        Id = id;
        Url = url;
    }

    public string Id { get; set; }

    public string Url { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is TweetIdentifier identifier && Equals(identifier);
    }

    public bool Equals(TweetIdentifier other)
    {
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public static bool operator ==(TweetIdentifier left, TweetIdentifier right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TweetIdentifier left, TweetIdentifier right)
    {
        return !(left == right);
    }

    public override string? ToString()
    {
        return Id ?? base.ToString();
    }
}
