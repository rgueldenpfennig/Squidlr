using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Squidlr.Api;
using Squidlr.Hosting.Extensions;

namespace Microsoft.AspNetCore.Builder;

public static class RateLimiterServiceCollectionExtensions
{
    public static IServiceCollection AddRateLimiterInternal(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.OnRejected = (ctx, ct) =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Client '{RemoteIpAddress}' has reached the rate limit", ctx.HttpContext.GetClientIpAddress() ?? "unknown");

                var builder = new StringBuilder();
                foreach (var header in ctx.HttpContext.Request.Headers)
                {
                    builder.AppendLine($"{header.Key}: {header.Value.ToString()}");
                }
                logger.LogWarning(builder.ToString());

                return ValueTask.CompletedTask;
            };

            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", partition =>
                        new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 60,
                            Window = TimeSpan.FromSeconds(30)
                        })),
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", partition =>
                        new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 6000,
                            Window = TimeSpan.FromHours(1)
                        })));

            options.AddPolicy("Content", ctx =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", partition =>
                    new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 3,
                        Window = TimeSpan.FromSeconds(60)
                    });
            });

            options.AddPolicy("Video", ctx =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", partition =>
                    new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 3,
                        Window = TimeSpan.FromSeconds(30)
                    });
            });
        });

        return services;
    }
}
