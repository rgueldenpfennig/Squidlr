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
            Assert.Equal(content.FullName, expectedContent.FullName);

            if (expectedContent.ProfilePictureUrl is not null)
            {
                Assert.Equal(content.ProfilePictureUrl!.AbsolutePath, expectedContent.ProfilePictureUrl.OriginalString);
            }

            for (var i = 0; i < expectedContent.Videos.Count; i++)
            {
                if (content.Videos.Count < (i + 1))
                    Assert.Fail("Expected video is missing");

                var expectedVideo = expectedContent.Videos[i];
                var video = content.Videos[i];

                Assert.Equal(video.DisplayUrl.AbsolutePath, expectedVideo.DisplayUrl.OriginalString);
                Assert.Equal(video.Duration, expectedVideo.Duration);
                Assert.Equal(video.Views, expectedVideo.Views);

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
        // default post with a single video
        yield return new object[]
        {
            new InstagramContent("https://www.instagram.com/p/aye83DjauH")
            {
                CreatedAtUtc = DateTimeOffset.Parse("2013-06-20T17:15:45+00:00", CultureInfo.InvariantCulture),
                FavoriteCount = 12076,
                FullText = "If Abel was a booger, Luke would definitely pick him. See it in action by playing this video. Yes! That's right, folks. This is a video and Instagram made that possible! Read about this new capability and check out the promo video (with our cameo appearance) by clicking on the link in my profile. Also, be sure to download the latest version of Instagram and start sharing your videos!",
                ReplyCount = 665,
                UserName = "naomipq",
                FullName = "B E A U T Y  F O R  A S H E S",
                ProfilePictureUrl = new Uri("/v/t51.2885-19/350847933_1258485001453274_5980656859626345933_n.jpg", UriKind.Relative),
                Videos = new InstagramVideoCollection
                {
                    new InstagramVideo
                    {
                        DisplayUrl = new Uri("/v/t51.2885-15/11379094_104911659849817_249670488_n.jpg", UriKind.Relative),
                        Duration = TimeSpan.Parse("00:00:08.7420000", CultureInfo.InvariantCulture),
                        Views = 0,
                        VideoSources = new VideoSourceCollection
                        {
                            new VideoSource
                            {
                                Bitrate = 0,
                                ContentLength = 1017460,
                                ContentType = "video/mp4",
                                Size = new(612, 612),
                                Url = new Uri("/o1/v/t16/f1/m84/684E26483F3B131A73D2F28B764A74AF_video_dashinit.mp4", UriKind.Relative)
                            }
                        }
                    }
                }
            }
        };

        // default reel with a single video
        yield return new object[]
        {
            new InstagramContent("https://www.instagram.com/marvelskies.fc/reel/CWqAgUZgCku/")
            {
                CreatedAtUtc = DateTimeOffset.Parse("2021-11-24T11:09:53+00:00", CultureInfo.InvariantCulture),
                FavoriteCount = 1011472,
                FullText = "Have no home 😂\n\n. Follow @marvelskies.fc \n\n.Tags 🏷️\n#spiderman #spidermannowayhome #tomholland #loveyou3000 #robertdowneyjr #andrewgarfield\n#tobeymaguire #ironman #marvel\n\n. Credits to the Respective Owners",
                ReplyCount = 1757,
                UserName = "marvelskies.fc",
                FullName = "Marvel Skies ©",
                ProfilePictureUrl = new Uri("/v/t51.2885-19/274309756_327224209336121_2039447280632515378_n.jpg", UriKind.Relative),
                Videos = new InstagramVideoCollection
                {
                    new InstagramVideo
                    {
                        DisplayUrl = new Uri("/v/t51.2885-15/260249305_1070857066995434_1652989255370474274_n.jpg", UriKind.Relative),
                        Duration = TimeSpan.Parse("00:00:08.7660000", CultureInfo.InvariantCulture),
                        Views = 9596788,
                        VideoSources = new VideoSourceCollection
                        {
                            new VideoSource
                            {
                                Bitrate = 0,
                                ContentLength = 1029283,
                                ContentType = "video/mp4",
                                Size = new(854, 480),
                                Url = new Uri("/v/t50.2886-16/259868018_1125905931280424_2234748865005605879_n.mp4", UriKind.Relative)
                            }
                        }
                    }
                }
            }
        };
    }
}
