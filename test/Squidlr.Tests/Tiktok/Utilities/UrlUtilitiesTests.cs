using Squidlr.Tiktok.Utilities;

namespace Squidlr.Tests.Tiktok.Utilities;

public class UrlUtilitiesTests
{
    [Theory]
    [InlineData("https://www.tiktok.com/@leenabhushan/video/6748451240264420610?foo=bar#abc", true)]
    [InlineData("https://www.tiktok.com/@patroxofficial/video/6742501081818877190?langCountry=en", true)]
    [InlineData("https://www.tiktok.com/@MS4wLjABAAAATh8Vewkn0LYM7Fo03iec3qKdeCUOcBIouRk1mkiag6h3o_pQu_dUXvZ2EZlGST7_/video/7042692929109986561", true)]
    [InlineData("https://www.tiktok.com/@pokemonlife22/video/7059698374567611694", true)]
    [InlineData("https://www.tiktok.com/@denidil6/video/7065799023130643713", true)]
    [InlineData("https://www.tiktok.com/@_le_cannibale_/video/7139980461132074283", true)]
    [InlineData("https://www.tiktok.com/@moxypatch/video/7206382937372134662", true)]
    [InlineData("http://tiktok.com/@moxypatch/video/7206382937372134662", true)]
    [InlineData("https://www.tiktok.com/@moxypatch/image/7206382937372134662", false)]
    [InlineData("https://example.com/foo/1152128691131318273/", false)]
    [InlineData("https://google.com", false)]
    [InlineData("invalid-url", false)]
    public void IsValidTiktokUrl(string url, bool expectedResult)
    {
        // Act
        var result = UrlUtilities.TryGetTiktokIdentifier(url, out _);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("https://www.tiktok.com/@leenabhushan/video/6748451240264420610?foo=bar#abc", "6748451240264420610", "https://www.tiktok.com/@leenabhushan/video/6748451240264420610")]
    [InlineData("https://www.tiktok.com/@patroxofficial/video/6742501081818877190?langCountry=en", "6742501081818877190", "https://www.tiktok.com/@patroxofficial/video/6742501081818877190")]
    [InlineData("http://tiktok.com/@moxypatch/video/7206382937372134662", "7206382937372134662", "http://tiktok.com/@moxypatch/video/7206382937372134662")]
    public void GetTiktokIdentifier(string url, string expectedId, string expectedUrl)
    {
        // Act
        Assert.True(UrlUtilities.TryGetTiktokIdentifier(url, out var identifier));

        // Assert
        Assert.Equal(expectedId, identifier?.Id);
        Assert.Equal(expectedUrl, identifier?.Url);
    }
}
