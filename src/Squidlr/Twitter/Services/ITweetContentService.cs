using DotNext;

namespace Squidlr.Twitter.Services
{
    public interface ITweetContentService
    {
        ValueTask<Result<TwitterContent, RequestVideoResult>> GetTweetContentAsync(TweetIdentifier identifier, CancellationToken cancellationToken);
    }
}