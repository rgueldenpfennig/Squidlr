namespace Squidlr.Api.IntegrationTests;

internal static class TestContext
{
    public static bool IsRunningOnGitHubActions => string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);
}
