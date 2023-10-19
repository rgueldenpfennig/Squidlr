namespace Squidlr.Abstractions;

public interface IUrlResolver
{
    ContentIdentifier ResolveUrl(string url);
}
