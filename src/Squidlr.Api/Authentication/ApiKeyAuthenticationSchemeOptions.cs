using Microsoft.AspNetCore.Authentication;

namespace Squidlr.Api.Authentication;

public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string? ApiKey { get; set; }
}

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}