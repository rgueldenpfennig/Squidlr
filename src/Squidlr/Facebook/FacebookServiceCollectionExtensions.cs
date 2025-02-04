using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidlr.Abstractions;

namespace Squidlr.Facebook;

public static class FacebookServiceCollectionExtensions
{
    public static IServiceCollection AddFacebook(this IServiceCollection services)
    {
        AddHttpClients(services);

        services.AddSingleton<FacebookWebClient>();
        services.AddSingleton<IUrlResolver, FacebookUrlResolver>();
        services.AddSingleton<IContentProvider, FacebookContentProvider>();

        return services;
    }

    private static void AddHttpClients(IServiceCollection services)
    {
        AddHttpClient(services, FacebookWebClient.HttpClientName, withProxy: false);
        AddHttpClient(services, FacebookWebClient.HttpClientWithProxyName, withProxy: true);
    }

    private static void AddHttpClient(IServiceCollection services, string httpClientName, bool withProxy)
    {
        services.AddHttpClient(httpClientName, (sp, client) =>
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
        }).ConfigurePrimaryHttpMessageHandler((sp) =>
        {
            var options = sp.GetRequiredService<IOptions<SquidlrOptions>>().Value;
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                UseProxy = false,
                UseCookies = true,
                AllowAutoRedirect = false
            };

            if (withProxy && options.ProxyOptions?.UseProxy == true)
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(options.ProxyOptions.ProxyAddress);
                handler.DefaultProxyCredentials = new NetworkCredential(options.ProxyOptions.UserName, options.ProxyOptions.Password);
            }

            return handler;
        });
    }
}
