using System.Globalization;
using System.Net;
using Squidlr.Instagram;
using Xunit.Abstractions;

namespace Squidlr.Api.IntegrationTests.Content;

public class InstagramContentRouteTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InstagramContentRouteTests(ApiWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        factory.TestOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-KEY", "foobar");
    }

    public static IEnumerable<object[]> TestData
            => CreateTestData();

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task RequestInstagramContent(InstagramContent expectedContent)
    {
        // Arrange & Act
        var response = await _client.GetAsync($"/content?url={expectedContent.SourceUrl}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<InstagramContent>();
            Assert.NotNull(content);
            Assert.Equal(content.CreatedAtUtc, expectedContent.CreatedAtUtc);
            Assert.InRange(content.FavoriteCount, expectedContent.FavoriteCount - 50, expectedContent.FavoriteCount + 50);
            Assert.InRange(content.ReplyCount, expectedContent.ReplyCount - 50, expectedContent.ReplyCount + 50);
            Assert.Equal(content.FullText, expectedContent.FullText);
            Assert.Equal(content.UserName, expectedContent.UserName);

            for (var i = 0; i < expectedContent.Videos.Count; i++)
            {
                if (content.Videos.Count < (i + 1))
                    Assert.Fail("Expected video is missing");

                var expectedVideo = expectedContent.Videos[i];
                var video = content.Videos[i];

                Assert.Equal(video.DisplayUrl.AbsolutePath, expectedVideo.DisplayUrl.OriginalString);
                Assert.Equal(video.Duration, expectedVideo.Duration);

                for (var j = 0; j < expectedVideo.VideoSources.Count; j++)
                {
                    if (expectedVideo.VideoSources.Count < (j + 1))
                        Assert.Fail("Expected video source is missing");

                    var expectedVideoSource = expectedVideo.VideoSources[j];
                    var videoSource = video.VideoSources[j];

                    Assert.Equal(videoSource.ContentType, expectedVideoSource.ContentType);
                    Assert.Equal(videoSource.ContentLength, expectedVideoSource.ContentLength);
                    Assert.Equal(videoSource.Size, expectedVideoSource.Size);
                    Assert.Equal(videoSource.Url.AbsolutePath, expectedVideoSource.Url.OriginalString);
                }
            }
        }
    }

    private static IEnumerable<object[]> CreateTestData()
    {
        yield return new object[]
        {
            new InstagramContent("https://www.instagram.com/p/aye83DjauH")
            {
                CreatedAtUtc = DateTimeOffset.Parse("2013-06-20T17:15:45+00:00", CultureInfo.InvariantCulture),
                FavoriteCount = 12076,
                FullText = "If Abel was a booger, Luke would definitely pick him. See it in action by playing this video. Yes! That's right, folks. This is a video and Instagram made that possible! Read about this new capability and check out the promo video (with our cameo appearance) by clicking on the link in my profile. Also, be sure to download the latest version of Instagram and start sharing your videos!",
                ReplyCount = 665,
                UserName = "naomipq",
                Videos = new InstagramVideoCollection
                {
                    new InstagramVideo
                    {
                        DisplayUrl = new Uri("/v/t51.2885-15/11379094_104911659849817_249670488_n.jpg", UriKind.Relative),
                        Duration = TimeSpan.Parse("00:00:08.7420000", CultureInfo.InvariantCulture),
                        VideoSources = new VideoSourceCollection
                        {
                            new VideoSource
                            {
                                Bitrate = 0,
                                ContentLength = 1017460,
                                ContentType = "video/mp4",
                                Size = VideoSize.Empty,
                                Url = new Uri("/o1/v/t16/f1/m84/684E26483F3B131A73D2F28B764A74AF_video_dashinit.mp4", UriKind.Relative)
                            }
                        }
                    }
                }
            }
        };
    }
}
