namespace Squidlr;

public static class SocialMediaPlatformExtensions
{
    public static HttpClient GetPlatformHttpClient(this IHttpClientFactory httpClientFactory, SocialMediaPlatform platform)
    {
        return platform switch
        {
            SocialMediaPlatform.Twitter => httpClientFactory.CreateClient(Twitter.TwitterWebClient.HttpClientName),
            SocialMediaPlatform.Instagram => httpClientFactory.CreateClient(Instagram.InstagramWebClient.HttpClientName),
            _ => throw new ArgumentOutOfRangeException(nameof(platform)),
        };
    }

    public static string GetPlatformName(this SocialMediaPlatform platform)
    {
        return platform switch
        {
            SocialMediaPlatform.Twitter => "X",
            SocialMediaPlatform.Instagram => "Instagram",
            _ => throw new ArgumentOutOfRangeException(nameof(platform)),
        };
    }
}