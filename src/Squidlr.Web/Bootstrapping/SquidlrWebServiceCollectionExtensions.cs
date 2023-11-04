using System.Net;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Squidlr;
using Squidlr.Abstractions;
using Squidlr.Instagram;
using Squidlr.Twitter;
using Squidlr.Web;
using Squidlr.Web.Clients;
using Squidlr.Web.States;
using Squidlr.Web.Telemetry;

namespace Microsoft.AspNetCore.Builder;

public static class SquidlrWebServiceCollectionExtensions
{
    public static IServiceCollection AddSquidlrWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AppState>();
        services.AddScoped<RequestVideoState>();
        services.AddScoped<VideoSearchQueryState>();
        services.AddOptions<ApplicationOptions>()
            .Bind(configuration.GetSection("Application"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ApiClient>();
        services.AddHttpClient(ApiClient.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ApplicationOptions>>().Value;

            client.BaseAddress = options.ApiHostUri;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultRequestHeaders.Add("X-API-KEY", options.ApiKey);
        })
        .AddPolicyHandler((services, request) => HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(new[]
            {
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromMilliseconds(300)
            },
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                services.GetService<ILogger<ApiClient>>()?
                    .LogWarning("Delaying for {delay}ms, then making retry {retry}.", timespan.TotalMilliseconds, retryAttempt);
            }
        ));

        services.AddScoped<TelemetryHandler>();
        services.AddScoped<ClipboardService>();


        services.AddSingleton(sp => new UrlResolver(
            sp.GetServices<IUrlResolver>().ToList().AsReadOnly()));

        services.AddSingleton<IUrlResolver, InstagramUrlResolver>();
        services.AddSingleton<IUrlResolver, TwitterUrlResolver>();

        return services;
    }
}
