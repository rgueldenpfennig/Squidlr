using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidlr.Abstractions;

namespace Squidlr.LinkedIn;

public static class LinkedInServiceCollectionExtensions
{
    public static IServiceCollection AddLinkedIn(this IServiceCollection services)
    {
        services.AddHttpClient(LinkedInWebClient.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SquidlrOptions>>().Value;

            client.DefaultRequestHeaders.Add("accept", "*/*");
            client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("sec-ch-ua", "Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \"Not:A-Brand\";v=\"99\"");
            client.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            client.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");

            client.BaseAddress = options.LinkedInHostUri;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.DefaultRequestVersion = HttpVersion.Version20;
        })
            .ConfigurePrimaryHttpMessageHandler((sp) =>
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    UseProxy = false,
                    UseCookies = false
                };

                return handler;
            });

        services.AddSingleton<LinkedInWebClient>();
        services.AddSingleton<IUrlResolver, LinkedInUrlResolver>();
        services.AddSingleton<IContentProvider, LinkedInContentProvider>();

        return services;
    }
}
