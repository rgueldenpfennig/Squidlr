using Microsoft.AspNetCore.Rewrite;

namespace Squidlr.Web.Bootstrapping;

public static class RewriteOptionsExtensions
{
    public static RewriteOptions AddRedirectToHost(this RewriteOptions options, string matchingDomain, string newDomain)
    {
        options.Rules.Add(new RedirectToHostRule(matchingDomain, newDomain));
        return options;
    }
}
