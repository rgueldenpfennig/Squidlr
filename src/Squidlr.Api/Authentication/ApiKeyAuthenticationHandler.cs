using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Squidlr.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private const string _apiKeyHeader = "X-API-KEY";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Options.ApiKey == null)
        {
            throw new InvalidOperationException($"{nameof(Options.ApiKey)} must not be null.");
        }

        // check for AllowAnonymous attribute on the endpoint
        var endpoint = Context.GetEndpoint();
        if (endpoint != null)
        {
            var allowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>();
            if (allowAnonymous != null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }

        if (!Context.Request.Headers.TryGetValue(_apiKeyHeader, out var apiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {_apiKeyHeader} header"));
        }

        if (Options.ApiKey!.Equals(apiKey, StringComparison.InvariantCulture))
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "Authenticated API key user") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail($"Invalid {_apiKeyHeader} header"));
    }
}
