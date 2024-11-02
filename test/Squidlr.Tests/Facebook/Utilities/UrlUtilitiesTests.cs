using Squidlr.Facebook.Utilities;

namespace Squidlr.Tests.Facebook.Utilities;

public class UrlUtilitiesTests
{
    [Theory]
    [InlineData("https://www.facebook.com/reel/356450730121408/?foo=bar#abc", true)]
    [InlineData("https://www.facebook.com/reel/356450730121408", true)]
    [InlineData("https://www.facebook.com/radiokicksfm/videos/3676516585958356/", true)]
    [InlineData("https://www.facebook.com/video.php?v=3676516585958356", true)]
    [InlineData("https://www.facebook.com/watch/?v=3676516585958356", true)]
    [InlineData("https://www.facebook.com/groups/1645456212344334/posts/3737828833107051", true)]
    [InlineData("https://example.com/foo/1152128691131318273/", false)]
    [InlineData("https://google.com", false)]
    [InlineData("invalid-url", false)]
    public void IsValidFacebookUrl(string url, bool expectedResult)
    {
        // Act
        var result = UrlUtilities.TryGetFacebookIdentifier(url, out _);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("https://www.facebook.com/reel/356450730121408/?foo=bar#abc", "356450730121408", "https://www.facebook.com/reel/356450730121408")]
    [InlineData("https://www.facebook.com/radiokicksfm/videos/3676516585958356/", "3676516585958356", "https://www.facebook.com/radiokicksfm/videos/3676516585958356")]
    [InlineData("https://www.facebook.com/video.php?v=3676516585958356", "3676516585958356", "https://www.facebook.com/video.php?v=3676516585958356")]
    [InlineData("https://www.facebook.com/watch/?v=3676516585958356", "3676516585958356", "https://www.facebook.com/watch/?v=3676516585958356")]
    [InlineData("https://www.facebook.com/groups/1645456212344334/posts/3737828833107051", "3737828833107051", "https://www.facebook.com/groups/1645456212344334/posts/3737828833107051")]
    public void GetFacebookIdentifier(string url, string expectedId, string expectedUrl)
    {
        // Act
        Assert.True(UrlUtilities.TryGetFacebookIdentifier(url, out var identifier));

        // Assert
        Assert.Equal(expectedId, identifier?.Id);
        Assert.Equal(expectedUrl, identifier?.Url);
    }
}