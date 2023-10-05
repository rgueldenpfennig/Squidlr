using DotNext;
using Microsoft.Extensions.Logging;

namespace Squidlr.Twitter.Services;

public class TweetContentService : ITweetContentService
{
    private readonly TweetContentParserFactory _tweetContentParserFactory;
    private readonly ILogger<TweetContentService> _logger;

    public TweetContentService(TweetContentParserFactory tweetContentParserFactory, ILogger<TweetContentService> logger)
    {
        _tweetContentParserFactory = tweetContentParserFactory ?? throw new ArgumentNullException(nameof(tweetContentParserFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async ValueTask<Result<TweetContent, GetTweetVideoResult>> GetTweetContentAsync(TweetIdentifier identifier, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Requesting Tweet content...");

        try
        {
            var parser = _tweetContentParserFactory.CreateTweetContentParser(identifier);
            var result = await parser.CreateTweetContentAsync(cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            return new(GetTweetVideoResult.Canceled);
        }
    }
}
