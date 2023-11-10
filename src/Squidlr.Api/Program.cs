using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using Squidlr.Hosting.Telemetry;
using Squidlr.Api.Authentication;
using Squidlr.Telemetry;
using Squidlr.Api.Telemetry;
using Microsoft.ApplicationInsights;

namespace Squidlr.Api;

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
            builder.Services.AddSingleton<ITelemetryService>(sp => new TelemetryService(sp.GetService<TelemetryClient>()));

            builder.Host.UseSquidlr();

            builder.Services.AddProblemDetails();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddResponseCompression();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddAuthentication("ApiKey")
                .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    "ApiKey",
                    opts => opts.ApiKey = builder.Configuration.GetValue<string>("Application:ApiKey"));
            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter();
            builder.Services.AddHealthChecks();

            var app = builder.Build();
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

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseForwardedHeaders();
            app.UseResponseCompression();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health").RequireHost("*:5001").AllowAnonymous();
            app.MapContentRoutes(app.Environment);
            app.MapVideoRoutes();

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