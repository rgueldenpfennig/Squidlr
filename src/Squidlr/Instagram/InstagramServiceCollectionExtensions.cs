using Microsoft.Extensions.DependencyInjection;
using Squidlr.Abstractions;

namespace Squidlr.Instagram;

internal static class InstagramServiceCollectionExtensions
{
    public static IServiceCollection AddInstagram(this IServiceCollection services)
    {
        //services.AddHttpClient(TwitterWebClient.HttpClientName, (sp, client) =>
        //{
        //    var options = sp.GetRequiredService<IOptions<SquidlrOptions>>().Value;

        //    client.DefaultRequestHeaders.Add("authorization", $"Bearer {options.AuthorizationBearerToken!}");
        //    client.DefaultRequestHeaders.Add("authority", "twitter.com");
        //    client.DefaultRequestHeaders.Add("accept", "*/*");
        //    client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");
        //    client.DefaultRequestHeaders.Add("sec-ch-ua", "Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \"Not:A-Brand\";v=\"99\"");
        //    client.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
        //    client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        //    client.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
        //    client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
        //    client.DefaultRequestHeaders.Add("x-twitter-active-user", "yes");
        //    client.DefaultRequestHeaders.Add("x-twitter-client-language", "en");

        //    client.BaseAddress = options.TwitterApiHostUri;
        //    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        //    client.DefaultRequestVersion = HttpVersion.Version20;
        //})
        //    .ConfigurePrimaryHttpMessageHandler(() =>
        //    {
        //        return new SocketsHttpHandler
        //        {
        //            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        //            UseCookies = false
        //        };
        //    })
        //    .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()
        //        .WaitAndRetryAsync(new[]
        //        {
        //            TimeSpan.FromMilliseconds(100),
        //            TimeSpan.FromMilliseconds(200),
        //            TimeSpan.FromMilliseconds(300)
        //        },
        //        onRetry: (outcome, timespan, retryAttempt, context) =>
        //        {
        //            services.GetService<ILogger<TwitterWebClient>>()?
        //                .LogWarning("Delaying for {delay}ms, then making retry {retry}.", timespan.TotalMilliseconds, retryAttempt);
        //        }
        //    ));

        //services.AddSingleton<TwitterWebClient>();
        //services.AddSingleton<TweetContentParserFactory>();
        //services.AddSingleton<TweetMediaService>();
        services.AddSingleton<IUrlResolver, InstagramUrlResolver>();
        //services.AddSingleton<IContentProvider, TwitterContentProvider>();

        return services;
    }
}
