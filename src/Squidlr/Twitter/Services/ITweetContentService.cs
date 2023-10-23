using DotNext;

namespace Squidlr.Twitter.Services
{
    public interface ITweetContentService
    {
        ValueTask<Result<TwitterContent, RequestContentResult>> GetTweetContentAsync(TweetIdentifier identifier, CancellationToken cancellationToken);
    }
}