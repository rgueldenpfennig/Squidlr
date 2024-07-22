using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Squidlr.Web.Clients;

public sealed class ClientDiscoveryService
{
    private readonly ApplicationOptions _options;

    public ClientDiscoveryService(IOptions<ApplicationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public Uri GetVideoStreamUrl(string contentUrl, string videoSelector)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentUrl);
        ArgumentException.ThrowIfNullOrEmpty(videoSelector);

        var query = new QueryBuilder
        {
            { "contentUrl", contentUrl },
            { "videoSelector", videoSelector }
        };

        return new UriBuilder(_options.ApiHostUri!)
        {
            Path = "/video",
            Query = query.ToString()
        }.Uri;
    }
}
