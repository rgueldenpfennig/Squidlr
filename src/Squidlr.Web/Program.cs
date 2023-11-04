using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Squidlr.Hosting.Telemetry;

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

            builder.Services.AddSquidlrWeb(builder.Configuration);

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

            if (app.Environment.IsProduction() || app.Environment.IsStaging())
            {
                app.UseExceptionHandler("/Error");
                app.UseResponseCompression();
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