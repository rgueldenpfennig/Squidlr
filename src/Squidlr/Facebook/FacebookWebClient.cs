namespace Squidlr.Facebook;

public sealed class FacebookWebClient : PlatformWebClient
{
    public const string HttpClientName = nameof(FacebookWebClient);

    public const string HttpClientWithProxyName = nameof(FacebookWebClient) + "Proxy";

    public FacebookWebClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory, HttpClientName)
    {
    }

    public Task<HttpResponseMessage> GetFacebookPostAsync(FacebookIdentifier identifier, CancellationToken cancellationToken) =>
        GetFacebookPostAsync(identifier, false, cancellationToken);

    public Task<HttpResponseMessage> GetFacebookPostAsync(FacebookIdentifier identifier, bool useProxy, CancellationToken cancellationToken) =>
        GetFacebookPostAsync(new Uri(identifier.Url, UriKind.Absolute), useProxy, cancellationToken);

    public Task<HttpResponseMessage> GetFacebookPostAsync(Uri uri, bool useProxy, CancellationToken cancellationToken)
    {
        var clientName = useProxy ? HttpClientWithProxyName : HttpClientName;
        var client = CreateClient(clientName);
        return client.GetAsync(uri, cancellationToken);
    }
}
