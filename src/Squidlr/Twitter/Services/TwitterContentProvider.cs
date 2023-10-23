using DotNext;
using Squidlr.Abstractions;
using Squidlr.Twitter.Utilities;

namespace Squidlr.Twitter.Services;

public class TwitterContentProvider : IContentProvider
{
    private readonly TweetContentParserFactory _tweetContentParserFactory;

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Twitter;

    public TwitterContentProvider(TweetContentParserFactory tweetContentParserFactory)
    {
        _tweetContentParserFactory = tweetContentParserFactory ?? throw new ArgumentNullException(nameof(tweetContentParserFactory));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var identifier = UrlUtilities.CreateTweetIdentifierFromUrl(url);
            var parser = _tweetContentParserFactory.CreateTweetContentParser(identifier);
            var result = await parser.CreateTweetContentAsync(cancellationToken);
            if (result.IsSuccessful)
            {
                return new(result.Value);
            }

            return new(result.Error);
        }
        catch (OperationCanceledException)
        {
            return new(RequestContentResult.Canceled);
        }
    }
}
