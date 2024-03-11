using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidlr.Abstractions;

namespace Squidlr.Tiktok;

internal static class TiktokServiceCollectionExtensions
{
    public static IServiceCollection AddTiktok(this IServiceCollection services)
    {
        services.AddHttpClient(TiktokWebClient.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SquidlrOptions>>().Value;

            client.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("sec-ch-ua", "Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \"Not:A-Brand\";v=\"99\"");
            client.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
            client.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
            client.DefaultRequestHeaders.Add("sec-fetch-site", "none");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");

            client.BaseAddress = options.TiktokHostUri;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.DefaultRequestVersion = HttpVersion.Version20;
        });

        services.AddSingleton<TiktokWebClient>();
        services.AddSingleton<IUrlResolver, TiktokUrlResolver>();
        services.AddSingleton<IContentProvider, TiktokContentProvider>();

        return services;
    }
}
