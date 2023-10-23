using DotNext;

namespace Squidlr.Abstractions;

public interface IContentProvider
{
    SocialMediaPlatform Platform { get; }

    ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken);
}
