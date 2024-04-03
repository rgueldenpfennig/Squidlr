namespace Squidlr;

public static class ContentIdentifierExtensions
{
    public static string GetSafeVideoFileName(this ContentIdentifier identifier, Uri videoUri)
    {
        return $"{identifier.Platform.GetPlatformName()}-{identifier.Id}-Squidlr{Path.GetExtension(videoUri.AbsolutePath)}";
    }
}
