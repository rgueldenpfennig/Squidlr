using Squidlr.Twitter.Utilities;

namespace Squidlr.Tests.Twitter.Utilities;

public class UrlUtilitiesTests
{
    [Theory]
    [InlineData("https://www.twitter.com/foo/status/1652178691131318273", true)]
    [InlineData("https://www.x.com/foo/status/1652178691131318273", true)]
    [InlineData("https://mobile.twitter.com/foo/status/1652178691131318273", true)]
    [InlineData("https://twitter.com/foo/status/1652178691131318273", true)]
    [InlineData("https://x.com/foo/status/1652178691131318273", true)]
    [InlineData("https://twitter.com/bar/status/1652356404882153473?s=20", true)]
    [InlineData("https://example.com/foo/1152128691131318273/", false)]
    [InlineData("https://twitter.com/foo/status/", false)]
    [InlineData("https://twitter.com/bar", false)]
    [InlineData("https://google.com", false)]
    [InlineData("invalid-url", false)]
    public void IsValidTwitterStatusUrl(string url, bool expectedResult)
    {
        // Act
        var result = UrlUtilities.IsValidTwitterStatusUrl(url);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("https://video.twimg.com/amplify_video/731129743244828672/vid/1280x720/PbHOy05lC7qXGG8B.mp4", true)]
    [InlineData("https://video.twimg.com/ext_tw_video/1652178579801735168/pu/vid/480x854/GfcYYvYAD6V4YxiV.mp4?tag=12", true)]
    [InlineData("https://video.twimg.com/foo/status/", false)]
    [InlineData("https://twitter.com/bar", false)]
    [InlineData("https://google.com", false)]
    [InlineData("invalid-url", false)]
    public void IsValidTwitterVideoUrl(string url, bool expectedResult)
    {
        // Act
        var result = UrlUtilities.IsValidTwitterVideoUrl(url);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("https://video.twimg.com/ext_tw_video/123456/pu/vid/480x854/abc.mp4?tag=12", 480, 854)]
    [InlineData("https://video.twimg.com/ext_tw_video/123456/pu/vid/320x568/abcabc.mp4?tag=12", 320, 568)]
    [InlineData("https://video.twimg.com/ext_tw_video/123456/pu/vid/638x1136/abc-abc.mp4?tag=12", 638, 1136)]
    [InlineData("https://video.twimg.com/tweet_video/FvTLoW8akAED-Kq.mp4", 0, 0)]
    public void ParseSizeFromVideoUrl(string url, int expectedWidth, int expectedHeight)
    {
        // Act
        var videoSize = UrlUtilities.ParseSizeFromVideoUrl(url);

        // Assert
        Assert.Equal(expectedWidth, videoSize.Width);
        Assert.Equal(expectedHeight, videoSize.Height);
    }

    [Theory]
    [InlineData("http://twitter.com/foo/status/1152128691131318273", "1152128691131318273", "http://twitter.com/foo/status/1152128691131318273")]
    [InlineData("https://twitter.com/foo/status/1152128691131318273", "1152128691131318273", "https://twitter.com/foo/status/1152128691131318273")]
    [InlineData("https://twitter.com/foo/status/1152128691131318273/", "1152128691131318273", "https://twitter.com/foo/status/1152128691131318273")]
    [InlineData("https://twitter.com/bar/status/1152128691131318273?s=20", "1152128691131318273", "https://twitter.com/bar/status/1152128691131318273")]
    [InlineData("https://twitter.com/bar/status/1152128691131318273?t=vwrUoo0dFecml2rC0EN99A", "1152128691131318273", "https://twitter.com/bar/status/1152128691131318273")]
    public void CreateTweetIdentifierFromUrl(string url, string expectedStatusId, string expectedStatusUrl)
    {
        // Act
        var result = UrlUtilities.CreateTweetIdentifierFromUrl(url);

        // Assert
        Assert.Equal(expectedStatusId, result.Id);
        Assert.Equal(expectedStatusUrl, result.Url);
    }
}
