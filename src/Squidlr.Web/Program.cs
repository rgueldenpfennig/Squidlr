using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using Squidlr.Hosting.Telemetry;
using Squidlr.Web.Clients;
using Squidlr.Web.States;
using Squidlr.Web.Telemetry;

namespace Squidlr.Web;

public partial class Program
{
    private static Serilog.ILogger _logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console()
                .CreateLogger()
                .ForContext<Program>();

    private Program()
    {
    }

    public static int Main(string[] args)
    {
        try
        {
            _logger.Information("Starting web host");
            _logger.Information(
                "Running with CLR {CLRVersion} on {OSVersion}",
                Environment.Version,
                Environment.OSVersion);

            var config = GetConfiguration(args);

            _logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateBootstrapLogger().ForContext<Program>();

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

            builder.Host.UseSerilog((context, serviceProvider, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(serviceProvider));

            builder.Services.AddTelemetry(o => o.IgnoreAbsolutePaths = new[] { "/health" });

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

            builder.Services.AddScoped<AppState>();
            builder.Services.AddScoped<RequestVideoState>();
            builder.Services.AddScoped<VideoSearchQueryState>();
            builder.Services.AddOptions<ApplicationOptions>()
                .Bind(builder.Configuration.GetSection("Application"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddScoped<ApiClient>();
            builder.Services.AddHttpClient(ApiClient.HttpClientName, (sp, client) =>
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

            builder.Services.AddScoped<TelemetryHandler>();
            builder.Services.AddScoped<ClipboardService>();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddRateLimiter();
            builder.Services.AddHealthChecks();

            var app = builder.Build();
            var applicationOptions = app.Services.GetRequiredService<IOptions<ApplicationOptions>>().Value;

            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (HttpContext ctx, double _, Exception? ex) => {
                    var defaultLevel = ex != null
                                            ? LogEventLevel.Error
                                            : ctx.Response.StatusCode > 499
                                                ? LogEventLevel.Error
                                                : LogEventLevel.Information;

                    return ctx.Request.Path == "/health" ? LogEventLevel.Debug : defaultLevel;
                };
            });
            app.UseResponseCompression();

            if (app.Environment.IsProduction())
            {
                app.UseExceptionHandler("/Error");
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders();
            app.UseSecurityHeaders(policies =>
                policies.AddDefaultSecurityHeaders()
                        .AddCustomHeader("Strict-Transport-Security", $"max-age={TimeSpan.FromDays(2 * 365).TotalSeconds}; includeSubDomains; preload")
            );

            app.UseStaticFiles();
            app.UseRouting();
            app.UseRateLimiter();
            app.MapHealthChecks("/health").RequireHost("*:5002");

            if (app.Environment.IsProduction())
            {
                app.UseRewriter(new RewriteOptions()
                   .AddRedirectToWww((int)HttpStatusCode.MovedPermanently, applicationOptions.Domain!));
            }

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
            return 0;
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Web host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IConfiguration GetConfiguration(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var configBuilder = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", false)
           .AddJsonFile($"appsettings.{environmentName}.json", true)
           .AddCommandLine(args)
           .AddEnvironmentVariables();

        return configBuilder.Build();
    }
}