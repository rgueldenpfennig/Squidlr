using Microsoft.Net.Http.Headers;
using Squidlr.Web.Telemetry;

namespace Squidlr.Web.Security;

public sealed class DisallowedUserAgentsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _disallowedUserAgents;

    public DisallowedUserAgentsMiddleware(RequestDelegate next, IEnumerable<string> disallowedUserAgents)
    {
        _next = next;
        _disallowedUserAgents = new HashSet<string>(disallowedUserAgents, StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context, TelemetryHandler telemetryHandler)
    {
        var userAgent = context.Request.Headers[HeaderNames.UserAgent].ToString();

        if (string.IsNullOrWhiteSpace(userAgent) ||
            _disallowedUserAgents.Contains(userAgent, StringComparer.OrdinalIgnoreCase) ||
            _disallowedUserAgents.Any(ua => userAgent.Contains(ua, StringComparison.OrdinalIgnoreCase)))
        {
            telemetryHandler.TrackEvent("RestrictedAccess", new Dictionary<string, string>
            {
                { "UserAgent", userAgent },
                { "Reason", "DisallowedUserAgent" },
            });

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Access has been restricted. Get in touch if you think this is an error.");
            return;
        }

        await _next(context);
    }
}

public static class DisallowedUserAgentsMiddlewareExtensions
{
    public static IApplicationBuilder UseDisallowedUserAgents(this IApplicationBuilder builder, IEnumerable<string> disallowedUserAgents)
    {
        return builder.UseMiddleware<DisallowedUserAgentsMiddleware>(disallowedUserAgents);
    }
}
