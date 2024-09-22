using System.Text.Encodings.Web;

namespace Squidlr.Instagram;

public sealed class InstagramWebClient : PlatformWebClient
{
    public const string HttpClientName = nameof(InstagramWebClient);

    public InstagramWebClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory, HttpClientName)
    {
    }

    public Task<HttpResponseMessage> GetInstagramPostAsync(InstagramIdentifier identifier, CancellationToken cancellationToken)
    {
        var relativeUrl = $"/graphql/query/?query_hash=9f8827793ef34641b2fb195d4d41151c&variables={UrlEncoder.Default.Encode($$"""{"shortcode":"{{identifier.Id}}"}""")}";
        var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Add("Referer", identifier.Url);

        var client = CreateClient();
        return client.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> GetInstagramResponseAsync(string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", url);

        var client = CreateClient();
        return client.SendAsync(request, cancellationToken);
    }
}
