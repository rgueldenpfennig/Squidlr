﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Instagram;
using Squidlr.Twitter;

namespace Squidlr;

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
                .ValidateOnStart();

            services.AddMemoryCache();
            services.AddSingleton(sp => new UrlResolver(
                sp.GetServices<IUrlResolver>().ToList().AsReadOnly()));
            services.AddSingleton(sp => new ContentProvider(
                sp.GetServices<IContentProvider>().ToList().AsReadOnly(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<ContentProvider>>()));

            // add supported social media platforms
            services.AddTwitter();
            services.AddInstagram();
        });

        return builder;
    }
}
