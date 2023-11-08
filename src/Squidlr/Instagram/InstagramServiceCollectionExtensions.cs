using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Squidlr.Abstractions;

namespace Squidlr.Instagram;

internal static class InstagramServiceCollectionExtensions
{
    public static IServiceCollection AddInstagram(this IServiceCollection services)
    {
        services.AddHttpClient(InstagramWebClient.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SquidlrOptions>>().Value;

            client.DefaultRequestHeaders.Add("accept", "*/*");
            client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("sec-ch-ua", "Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \"Not:A-Brand\";v=\"99\"");
            client.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            client.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("X-Ig-App-Id", "936619743392459");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            client.BaseAddress = options.InstagramHostUri;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.DefaultRequestVersion = HttpVersion.Version20;
        })
            .ConfigurePrimaryHttpMessageHandler((sp) =>
            {
                var options = sp.GetRequiredService<IOptions<SquidlrOptions>>().Value;
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    UseProxy = false,
                    UseCookies = false
                };

                if (options.ProxyOptions?.UseProxy == true)
                {
                    handler.UseProxy = true;
                    handler.Proxy = new WebProxy(options.ProxyOptions.ProxyAddress);
                    handler.DefaultProxyCredentials = new NetworkCredential(options.ProxyOptions.UserName, options.ProxyOptions.Password);
                }

                return handler;
            })
            .AddPolicyHandler((services, request) => HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => response.StatusCode == HttpStatusCode.Unauthorized)
                .WaitAndRetryAsync(
                retryCount: 6,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(50),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    services.GetService<ILogger<InstagramWebClient>>()?
                        .LogWarning("Delaying for {delay}ms, then making retry {retry}.", timespan.TotalMilliseconds, retryAttempt);
                }
            ));

        services.AddSingleton<InstagramWebClient>();
        services.AddSingleton<IUrlResolver, InstagramUrlResolver>();
        services.AddSingleton<IContentProvider, InstagramContentProvider>();

        return services;
    }
}
