using Squidlr.Instagram.Utilities;

namespace Squidlr.Tests.Instagram.Utilities;

public class UrlUtilitiesTests
{
    [Theory]
    [InlineData("https://instagram.com/p/aye83DjauH/?foo=bar#abc", true)]
    [InlineData("https://www.instagram.com/reel/Chunk8-jurw/", true)]
    [InlineData("https://www.instagram.com/p/BQ0eAlwhDrw/", true)]
    [InlineData("https://www.instagram.com/tv/BkfuX9UB-eK/", true)]
    [InlineData("http://instagram.com/p/9o6LshA7zy/embed/", true)]
    [InlineData("https://www.instagram.com/stories/highlights/18090946048123978/", false)] // TODO: Support Instagram story URLs
    [InlineData("https://www.instagram.com/marvelskies.fc/reel/CWqAgUZgCku/", true)]
    [InlineData("https://www.instagram.com/marvelskies.fc/abc/CWqAgUZgCku/", false)]
    [InlineData("https://example.com/foo/1152128691131318273/", false)]
    [InlineData("https://google.com", false)]
    [InlineData("invalid-url", false)]
    public void IsValidInstagramUrl(string url, bool expectedResult)
    {
        // Act
        var result = UrlUtilities.IsValidInstagramUrl(url);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("https://instagram.com/p/aye83DjauH/?foo=bar#abc", "aye83DjauH", "https://instagram.com/p/aye83DjauH")]
    [InlineData("https://www.instagram.com/reel/Chunk8-jurw/", "Chunk8-jurw", "https://www.instagram.com/reel/Chunk8-jurw")]
    [InlineData("https://www.instagram.com/p/BQ0eAlwhDrw/", "BQ0eAlwhDrw", "https://www.instagram.com/p/BQ0eAlwhDrw")]
    [InlineData("https://www.instagram.com/tv/BkfuX9UB-eK/", "BkfuX9UB-eK", "https://www.instagram.com/tv/BkfuX9UB-eK")]
    [InlineData("http://instagram.com/p/9o6LshA7zy/embed/", "9o6LshA7zy", "http://instagram.com/p/9o6LshA7zy")]
    public void GetInstagramIdentifier(string url, string expectedId, string expectedUrl)
    {
        // Act
        var identifier = UrlUtilities.GetInstagramIdentifier(url);

        // Assert
        Assert.Equal(expectedId, identifier.Id);
        Assert.Equal(expectedUrl, identifier.Url);
    }
}
