using DotNext;

namespace Squidlr.Twitter.Services
{
    public interface ITweetContentService
    {
        ValueTask<Result<TweetContent, GetTweetVideoResult>> GetTweetContentAsync(TweetIdentifier identifier, CancellationToken cancellationToken);
    }
}