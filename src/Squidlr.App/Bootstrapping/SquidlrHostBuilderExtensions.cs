using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Squidlr;
using Squidlr.Abstractions;
using Squidlr.Instagram;
using Squidlr.Telemetry;
using Squidlr.Twitter;

namespace Microsoft.Maui.Hosting;

public static class SquidlrHostBuilderExtensions
{
    public static MauiAppBuilder UseSquidlr(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

    //    "Squidlr": {
    //        "InstagramHostUri": "https://www.instagram.com",
    //"TwitterAuthorizationBearerToken": "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA",
    //"TwitterApiHostUri": "https://api.twitter.com",
    //"ProxyOptions": {
    //            "UseProxy": true
    //}
    //    }

        var services = builder.Services;
        services.AddSingleton(new SquidlrOptions
        {
            InstagramHostUri = new Uri("https://www.instagram.com"),
            TwitterApiHostUri = new Uri("https://api.twitter.com"),
            TwitterAuthorizationBearerToken = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA"
        });

        //services.AddLogging((a) => a.AddProvider())

        services.AddMemoryCache();
        services.AddSingleton(sp => new UrlResolver(
            sp.GetServices<IUrlResolver>().ToList().AsReadOnly()));
        services.AddSingleton(sp => new ContentProvider(
            sp.GetServices<IContentProvider>().ToList().AsReadOnly(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<ITelemetryService>(),
            sp.GetRequiredService<ILogger<ContentProvider>>()));

        // add supported social media platforms
        services.AddTwitter();
        services.AddInstagram();

        return builder;
    }
}
