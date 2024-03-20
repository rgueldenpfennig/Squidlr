using CommunityToolkit.Maui;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Squidlr;
using Squidlr.Abstractions;
using Squidlr.App.Pages;
using Squidlr.App.Telemetry;
using Squidlr.Instagram;
using Squidlr.Telemetry;
using Squidlr.Twitter;

namespace Microsoft.Maui.Hosting;

public static class SquidlrHostBuilderExtensions
{
    public static MauiAppBuilder UseSquidlr(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        // pages and view models
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainPageViewModel>();

        builder.Services.AddTransient<DownloadPage>();
        builder.Services.AddTransient<DownloadPageViewModel>();

        services.AddSingleton(new SquidlrOptions
        {
            InstagramHostUri = new Uri("https://www.instagram.com"),
            TwitterApiHostUri = new Uri("https://api.twitter.com"),
            TwitterAuthorizationBearerToken = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA"
        });

        //services.AddLogging((a) => a.AddProvider())

        services.AddSingleton<ITelemetryService>(new TelemetryService());

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
