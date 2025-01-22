using Squidlr.LinkedIn.Utilities;

namespace Squidlr.Tests.LinkedIn.Utilities;

public class UrlUtilitiesTests
{
    [Theory]
    [InlineData("https://www.linkedin.com/posts/william-is-saying-ugcPost-7210720852944867329-nmqI/?foo=bar#abc", true)]
    [InlineData("https://www.linkedin.com/posts/foo-7210720852944867329-foo", true)]
    [InlineData("https://linkedin.com/posts/foo-54534225453422-abc", true)]
    [InlineData("https://www.linkedin.com/posts/foo-bar-371338145_abc-barfoo-ugcPost-7281420308538407825-l-o1", true)]
    [InlineData("https://linkedin.com/video/foo-5453422-abc", false)]
    [InlineData("https://linkedin.com/video/foo-abc", false)]
    [InlineData("https://example.com/foo/1152128691131318273/", false)]
    [InlineData("https://google.com", false)]
    [InlineData("invalid-url", false)]
    public void IsValidLinkedInUrl(string url, bool expectedResult)
    {
        // Act
        var result = UrlUtilities.TryGetLinkedInIdentifier(url, out _);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("https://www.linkedin.com/posts/william-is-saying-ugcPost-7210720852944867329-nmqI/?foo=bar#abc", "7210720852944867329", "https://www.linkedin.com/posts/william-is-saying-ugcPost-7210720852944867329-nmqI")]
    [InlineData("https://www.linkedin.com/posts/foo-bar-371338145_abc-barfoo-ugcPost-7281420308538407825-l-o1", "7281420308538407825", "https://www.linkedin.com/posts/foo-bar-371338145_abc-barfoo-ugcPost-7281420308538407825-l-o1")]
    public void GetLinkedInIdentifier(string url, string expectedId, string expectedUrl)
    {
        // Act
        Assert.True(UrlUtilities.TryGetLinkedInIdentifier(url, out var identifier));

        // Assert
        Assert.Equal(expectedId, identifier.Value.Id);
        Assert.Equal(expectedUrl, identifier.Value.Url);
    }
}
