namespace Squidlr.Instagram;

public sealed class InstagramWebClient
{
    public const string HttpClientName = nameof(InstagramWebClient);

    private readonly IHttpClientFactory _clientFactory;

    public InstagramWebClient(IHttpClientFactory httpClientFactory)
    {
        _clientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Add("Referer", requestUri);

        return client.GetAsync(requestUri, cancellationToken);
    }

    public async ValueTask<long?> GetVideoContentLengthAsync(Uri videoFileUri, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(HttpClientName);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Head, videoFileUri);
        using var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response.Content.Headers.ContentLength;
        }

        return null;
    }
}
