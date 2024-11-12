using System.Globalization;
using System.IO.Compression;
using System.Net;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Events;
using Squidlr.Hosting.Compression;
using Squidlr.Hosting.Telemetry;
using Squidlr.Web.Bootstrapping;
using Squidlr.Web.Telemetry;

namespace Squidlr.Web;

public partial class Program
{
    private static Serilog.ILogger _logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
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
            var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(config);

            _logger = loggerConfig.CreateBootstrapLogger().ForContext<Program>();

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

            var addTelemetry = config["APPLICATIONINSIGHTS_CONNECTION_STRING"] is not null;
            if (addTelemetry)
            {
                builder.Services.AddTelemetry(o => o.IgnoreAbsolutePaths = ["/health"]);
                builder.Services.AddSingleton<ITelemetryInitializer, AppStateTelemetryInitializer>();
            }

            builder.Host.UseSerilog((context, serviceProvider, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration)
                             .ReadFrom.Services(serviceProvider);

                if (addTelemetry)
                {
                    configuration.WriteTo.ApplicationInsights(
                        serviceProvider.GetRequiredService<TelemetryConfiguration>(),
                        TelemetryConverter.Traces);
                }
            });

            // Add services to the container.
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddRazorPages();
            builder.Services.AddRazorComponents()
                            .AddInteractiveServerComponents();
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "X-XSRF-ANTIFORGERY";
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
                options.HeaderName = "X-XSRF-TOKEN";
                options.SuppressXFrameOptionsHeader = false;
            });

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<ZstdCompressionProvider>();
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml"]);
            });

            builder.Services.Configure<ZstdCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            builder.Services.AddSquidlrWeb(builder.Configuration);

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddRateLimiterInternal();
            builder.Services.AddHealthChecks();

            var app = builder.Build();
            var applicationOptions = app.Services.GetRequiredService<IOptions<ApplicationOptions>>().Value;

            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticsContext, httpContext) =>
                {
                    var request = httpContext.Request;
                    if (request.Headers.UserAgent != StringValues.Empty)
                    {
                        diagnosticsContext.Set(HeaderNames.UserAgent, request.Headers.UserAgent.ToString());
                    }
                };

                options.GetLevel = (HttpContext ctx, double _, Exception? ex) =>
                {
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
                        .AddContentSecurityPolicy(builder =>
                         {
                             builder.AddDefaultSrc().None();

                             if (app.Environment.IsDevelopment())
                             {
                                 builder.AddConnectSrc()
                                        .Self()
                                        .From("http://localhost:*")
                                        .From("https://localhost:*")
                                        .From("ws://localhost:*")
                                        .From("wss://localhost:*");
                             }
                             else
                             {
                                 builder.AddConnectSrc().Self();
                             }

                             builder.AddBaseUri().Self();
                             builder.AddFontSrc().Self();
                             builder.AddFormAction().Self();
                             builder.AddFrameAncestors().None();
                             builder.AddImgSrc().Self().From("https://*.tiktokcdn.com")
                                                       .From("https://media.licdn.com")
                                                       .From("https://pbs.twimg.com");
                             builder.AddObjectSrc().None();
                             builder.AddScriptSrc().Self();
                             builder.AddStyleSrc().Self();
                             builder.AddStyleSrcAttr().Self().UnsafeInline();
                             builder.AddStyleSrcElem().Self();
                         })
                        .AddCustomHeader("Strict-Transport-Security", $"max-age={TimeSpan.FromDays(2 * 365).TotalSeconds}; includeSubDomains; preload")
            );

            app.UseStaticFiles();
            app.UseRouting();
            app.UseStatusCodePagesWithRedirects("/404");
            app.UseAntiforgery();
            app.UseRateLimiter();
            app.MapHealthChecks("/health").RequireHost("*:5002");

            if (app.Environment.IsProduction())
            {
                app.UseRewriter(new RewriteOptions()
                   .AddRedirectToWww((int)HttpStatusCode.MovedPermanently, applicationOptions.Domain!)
                   .AddRedirectToHost(applicationOptions.InternalDomain!, applicationOptions.Domain!));
            }

            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

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
           .AddUserSecrets<Program>()
           .AddEnvironmentVariables();

        return configBuilder.Build();
    }
}