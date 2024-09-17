namespace Squidlr;

public abstract class PlatformWebClient
{
    private readonly string _httpClientName;
    private readonly IHttpClientFactory _clientFactory;

    protected PlatformWebClient(IHttpClientFactory httpClientFactory, string httpClientName)
    {
        if (string.IsNullOrEmpty(httpClientName))
        {
            throw new ArgumentException($"'{nameof(httpClientName)}' cannot be null or empty.", nameof(httpClientName));
        }

        _httpClientName = httpClientName;
        _clientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    protected HttpClient CreateClient()
    {
        return _clientFactory.CreateClient(_httpClientName);
    }

    public async ValueTask<(long?, string?)> GetVideoContentLengthAndMediaTypeAsync(Uri videoFileUri, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Head, videoFileUri);
        using var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (response.Content.Headers.ContentLength, response.Content.Headers.ContentType?.MediaType);
        }

        return (null, null);
    }
}
