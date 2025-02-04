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
        services.AddRateLimiter(static options =>
        {
            options.OnRejected = static (ctx, ct) =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Client '{RemoteIpAddress}' has reached the rate limit due to lease '{RateLimitLease}'", ctx.HttpContext.GetClientIpAddress() ?? "unknown", ctx.Lease);

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
                PartitionedRateLimiter.Create<HttpContext, string>(static ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", static partition =>
                        new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromHours(1)
                        })),
                PartitionedRateLimiter.Create<HttpContext, string>(static ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", static partition =>
                        new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 200,
                            Window = TimeSpan.FromHours(24)
                        })));

            options.AddPolicy("Content", static ctx =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", static partition =>
                    new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 6,
                        Window = TimeSpan.FromSeconds(120)
                    });
            });

            options.AddPolicy("Video", static ctx =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", static partition =>
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
