using System.Text.Encodings.Web;

namespace Squidlr.Instagram;

public sealed class InstagramWebClient
{
    public const string HttpClientName = nameof(InstagramWebClient);

    private readonly IHttpClientFactory _clientFactory;

    public InstagramWebClient(IHttpClientFactory httpClientFactory)
    {
        _clientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public Task<HttpResponseMessage> GetInstagramPostAsync(InstagramIdentifier identifier, CancellationToken cancellationToken)
    {
        var relativeUrl = $"/graphql/query/?query_hash=9f8827793ef34641b2fb195d4d41151c&variables={UrlEncoder.Default.Encode($$"""{"shortcode":"{{identifier.Id}}"}""")}";
        var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Add("Referer", identifier.Url);

        var client = _clientFactory.CreateClient(HttpClientName);

        return client.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> GetInstagramResponseAsync(string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", url);

        var client = _clientFactory.CreateClient(HttpClientName);

        return client.SendAsync(request, cancellationToken);
    }

    public async ValueTask<(long?, string?)> GetVideoContentLengthAndTypeAsync(Uri videoFileUri, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(HttpClientName);
        var requestMessage = new HttpRequestMessage(HttpMethod.Head, videoFileUri);
        using var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (response.Content.Headers.ContentLength, response.Content.Headers.ContentType?.MediaType);
        }

        return (null, null);
    }
}
