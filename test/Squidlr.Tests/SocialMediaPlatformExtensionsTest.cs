using NSubstitute;
using Squidlr.Instagram;
using Squidlr.Twitter;

namespace Squidlr.Tests;

public class SocialMediaPlatformExtensionsTest
{
    [Theory]
    [InlineData(SocialMediaPlatform.Twitter, TwitterWebClient.HttpClientName)]
    [InlineData(SocialMediaPlatform.Instagram, InstagramWebClient.HttpClientName)]
    public void GetPlatformHttpClient_WhenCalled_ReturnsHttpClientWithCorrectHttpClientName(SocialMediaPlatform platform, string expected)
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient();
        httpClientFactory.CreateClient(expected).Returns(httpClient);

        // Act
        var result = httpClientFactory.GetPlatformHttpClient(platform);

        // Assert
        Assert.Equal(httpClient, result);
    }

    [Fact]
    public void GetPlatformHttpClient_WhenCalledWithUnknownPlatform_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();

        // Act
        void Act() => httpClientFactory.GetPlatformHttpClient(SocialMediaPlatform.Unknown);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(Act);
    }

    [Theory]
    [InlineData(SocialMediaPlatform.Twitter, "X")]
    [InlineData(SocialMediaPlatform.Instagram, "Instagram")]
    public void GetPlatformName_WhenCalled_ReturnsPlatformName(SocialMediaPlatform platform, string expected)
    {
        // Act
        var result = platform.GetPlatformName();

        // Assert
        Assert.Equal(expected, result);
    }

    // add test for GetPlatformName and assert ArgumentOutOfRangeException
    [Fact]
    public void GetPlatformName_WhenCalledWithUnknownPlatform_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var platform = SocialMediaPlatform.Unknown;

        // Act
        void Act() => platform.GetPlatformName();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(Act);
    }
}
