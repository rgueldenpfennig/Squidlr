namespace Squidlr;

public readonly struct VideoSelector
{
    public readonly int Index { get; init; }

    public VideoSelector() : this(0)
    {
    }

    public VideoSelector(int index)
    {
        Index = index;
    }

    public static VideoSelector Default => new VideoSelector();
}
