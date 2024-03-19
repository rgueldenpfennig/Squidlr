using Microsoft.Extensions.Caching.Memory;
using Squidlr;
using Squidlr.Abstractions;
using Squidlr.Instagram;
using Squidlr.Telemetry;
using Squidlr.Twitter;

namespace Microsoft.AspNetCore.Builder;

public static class SquidlrHostBuilderExtensions
{
    public static IHostBuilder UseSquidlr(this IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices((ctx, services) =>
        {
            services.AddOptions<SquidlrOptions>()
                .Bind(ctx.Configuration.GetSection("Squidlr"))
                .ValidateDataAnnotations()
                .ValidateOnStart()
                .PostConfigure<IServiceProvider>((options, sp) =>
                {
                    var logger = sp.GetRequiredService<ILogger<SquidlrOptions>>();
                    if (options.ProxyOptions?.UseProxy == true)
                    {
                        logger.LogInformation("Using proxy: {ProxyAddress}", options.ProxyOptions.ProxyAddress);
                    }
                    else
                    {
                        logger.LogInformation("No proxy configured.");
                    }
                });

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
        });

        return builder;
    }
}
