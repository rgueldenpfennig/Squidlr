using Microsoft.AspNetCore.Http;

namespace Squidlr.Hosting.Extensions;

public static class HttpContextExtensions
{
    private static readonly string _clientIpHeader = "x-forwarded-for";

    public static string? GetClientIpAddress(this HttpContext context)
    {
        var headers = context.Request.Headers;
        if (headers.ContainsKey(_clientIpHeader))
        {
            return headers[_clientIpHeader];
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
