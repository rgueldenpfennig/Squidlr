namespace Squidlr.LinkedIn;

public sealed class LinkedInWebClient
{
    public const string HttpClientName = nameof(LinkedInWebClient);

    private readonly IHttpClientFactory _clientFactory;

    public LinkedInWebClient(IHttpClientFactory httpClientFactory)
    {
        _clientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public Task<HttpResponseMessage> GetLinkedInPostAsync(LinkedInIdentifier identifier, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(HttpClientName);
        return client.GetAsync(identifier.Url, cancellationToken);
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
