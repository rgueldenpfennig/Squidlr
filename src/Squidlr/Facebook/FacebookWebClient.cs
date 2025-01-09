namespace Squidlr.Facebook;

public sealed class FacebookWebClient : PlatformWebClient
{
    public const string HttpClientName = nameof(FacebookWebClient);

    public FacebookWebClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory, HttpClientName)
    {
    }

    public Task<HttpResponseMessage> GetFacebookPostAsync(FacebookIdentifier identifier, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        return client.GetAsync(identifier.Url, cancellationToken);
    }
}
