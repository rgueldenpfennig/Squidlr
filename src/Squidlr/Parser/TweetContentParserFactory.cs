using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Squidlr.Client;

namespace Squidlr.Parser;

public sealed class TweetContentParserFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TweetContentParserFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TweetContentParser CreateTweetContentParser(TweetIdentifier tweetIdentifier)
    {
        var twitterClient = _serviceProvider.GetRequiredService<TwitterWebClient>();
        var logger = _serviceProvider.GetRequiredService<ILogger<TweetContentParser>>();

        return new TweetContentParser(tweetIdentifier, twitterClient, logger);
    }
}
