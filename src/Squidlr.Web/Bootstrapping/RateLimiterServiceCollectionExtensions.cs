using System.Net;
using System.Threading.RateLimiting;
using Squidlr.Hosting.Extensions;
using Squidlr.Web;

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
                return ValueTask.CompletedTask;
            };

            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(ctx.GetClientIpAddress() ?? "unknown", partition =>
                        new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 120,
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
        });

        return services;
    }
}
