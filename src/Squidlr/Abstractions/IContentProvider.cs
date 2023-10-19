using DotNext;

namespace Squidlr.Abstractions;

public interface IContentProvider
{
    ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken);
}
