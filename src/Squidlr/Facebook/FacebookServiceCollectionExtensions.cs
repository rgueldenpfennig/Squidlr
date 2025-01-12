using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidlr.Abstractions;

namespace Squidlr.Facebook;

public static class FacebookServiceCollectionExtensions
{
    public static IServiceCollection AddFacebook(this IServiceCollection services)
    {
        services.AddHttpClient(FacebookWebClient.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SquidlrOptions>>().Value;

            client.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("DNT", "1");
            client.DefaultRequestHeaders.Add("Sec-GPC", "1");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
            client.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
            client.DefaultRequestHeaders.Add("sec-fetch-site", "cross-site");
            client.DefaultRequestHeaders.Add("Priority", "u=0, i");
            client.DefaultRequestHeaders.Add("TE", "trailers");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0");

            client.BaseAddress = options.FacebookHostUri;
        })
            .ConfigurePrimaryHttpMessageHandler((sp) =>
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    UseProxy = false,
                    UseCookies = true
                };

                return handler;
            });

        services.AddSingleton<FacebookWebClient>();
        services.AddSingleton<IUrlResolver, FacebookUrlResolver>();
        services.AddSingleton<IContentProvider, FacebookContentProvider>();

        return services;
    }
}
