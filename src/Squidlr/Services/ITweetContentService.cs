using DotNext;

namespace Squidlr.Services
{
    public interface ITweetContentService
    {
        ValueTask<Result<TweetContent, GetTweetVideoResult>> GetTweetContentAsync(TweetIdentifier identifier, CancellationToken cancellationToken);
    }
}