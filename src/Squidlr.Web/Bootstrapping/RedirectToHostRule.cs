using System.Net;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite;

namespace Squidlr.Web.Bootstrapping;

internal sealed class RedirectToHostRule : IRule
{
    private readonly string _matchingDomain;
    private readonly string _newDomain;

    public RedirectToHostRule(string matchingDomain, string newDomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchingDomain);
        ArgumentException.ThrowIfNullOrWhiteSpace(newDomain);

        _matchingDomain = matchingDomain;
        _newDomain = newDomain;
    }

    public void ApplyRule(RewriteContext context)
    {
        if (context.HttpContext.Request.Host.Host.Equals(_matchingDomain, StringComparison.OrdinalIgnoreCase))
        {
            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;

            var newUrl = UriHelper.BuildAbsolute(
                request.Scheme,
                new HostString(_newDomain),
                request.PathBase,
                request.Path,
                request.QueryString);

            response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            response.Headers.Location = newUrl;
            context.Result = RuleResult.EndResponse;
        }

        context.Result = RuleResult.ContinueRules;
    }
}
