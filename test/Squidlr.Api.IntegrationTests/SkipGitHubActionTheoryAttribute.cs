namespace Squidlr.Api.IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipGitHubActionTheoryAttribute : TheoryAttribute
{
    public SkipGitHubActionTheoryAttribute()
    {
        if (TestContext.IsRunningOnGitHubActions)
        {
            Skip = "Test is disabled on GitHub Actions.";
        }
    }
}
