namespace Squidlr.Instagram;

public sealed class InstagramWebClient : PlatformWebClient
{
    public const string HttpClientName = nameof(InstagramWebClient);

    public InstagramWebClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory, HttpClientName)
    {
    }

    public Task<HttpResponseMessage> GetInstagramPostAsync(InstagramIdentifier identifier, CancellationToken cancellationToken)
    {
        var client = CreateClient();

        var graphRequest = new HttpRequestMessage(HttpMethod.Post, "/graphql/query");
        graphRequest.Headers.Add("Referer", identifier.Url);

        var data = new Dictionary<string, string>
        {
            { "doc_id", "8845758582119845" },
            { "variables", $"{{\"shortcode\":\"{identifier.Id}\",\"fetch_tagged_user_count\":null,\"hoisted_comment_id\":null,\"hoisted_reply_id\":null}}" }
        };
        var graphContent = new FormUrlEncodedContent(data);
        graphRequest.Content = graphContent;

        return client.SendAsync(graphRequest, cancellationToken);
    }

    public Task<HttpResponseMessage> GetInstagramResponseAsync(string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", url);

        var client = CreateClient();
        return client.SendAsync(request, cancellationToken);
    }
}
