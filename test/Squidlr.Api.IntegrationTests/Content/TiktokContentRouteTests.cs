using System.Globalization;
using System.Net;
using Squidlr.Tiktok;
using Xunit.Abstractions;

namespace Squidlr.Api.IntegrationTests.Content;

public class TiktokContentRouteTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TiktokContentRouteTests(ApiWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        factory.TestOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-KEY", "foobar");
    }

    public static IEnumerable<object[]> TestData
            => CreateTestData();

    [SkipGitHubActionTheory]
    [MemberData(nameof(TestData))]
    public async Task RequestTiktokContent(TiktokContent expectedContent)
    {
        // Arrange & Act
        var response = await _client.GetAsync($"/content?url={expectedContent.SourceUrl}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<TiktokContent>();
            Assert.NotNull(content);
            Assert.Equal(content.CreatedAtUtc, expectedContent.CreatedAtUtc);
            Assert.InRange(content.FavoriteCount, expectedContent.FavoriteCount - 100, expectedContent.FavoriteCount + 100);
            Assert.InRange(content.ReplyCount, expectedContent.ReplyCount - 100, expectedContent.ReplyCount + 100);
            Assert.Equal(content.FullText, expectedContent.FullText);
            Assert.Equal(content.Username, expectedContent.Username);

            for (var i = 0; i < expectedContent.Videos.Count; i++)
            {
                if (content.Videos.Count < (i + 1))
                    Assert.Fail("Expected video is missing");

                var expectedVideo = expectedContent.Videos[i];
                var video = content.Videos[i];

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
                }
            }
        }
    }

    private static IEnumerable<object[]> CreateTestData()
    {
        // default post with a single video
        yield return new object[]
        {
            new TiktokContent("https://www.tiktok.com/@euro2024/video/7391624670811933984")
            {
                CreatedAtUtc = DateTimeOffset.Parse("2024-07-14T22:39:01+00:00", CultureInfo.InvariantCulture),
                FullText = "Yamal 🤩💫 #EURO2024 #SpainvsEngland #Spain ",
                FavoriteCount = 1_400_000,
                PlayCount = 10_100_000,
                CollectCount = 87_718,
                ReplyCount = 6_752,
                Username = "euro2024",
                Videos = new VideoCollection
                {
                    new Video
                    {
                        Duration = TimeSpan.Parse("00:00:09", CultureInfo.InvariantCulture),
                        VideoSources = new VideoSourceCollection
                        {
                            new VideoSource
                            {
                                Bitrate = 2303571,
                                ContentLength = 2601308,
                                ContentType = "video/mp4",
                                Size = new(1024, 576),
                                Url = new Uri("https://www.tiktok.com/aweme/v1/play/?faid=1988&file_id=9c57184dca0546fe9f2530c841e32282&is_play_url=1&item_id=7391624670811933984&line=0&ply_type=2&signaturev3=dmlkZW9faWQ7ZmlsZV9pZDtpdGVtX2lkLjE3OWE5Y2Q5OWExMTIwNzFmYzc0ZDU2OTY5ZTU2ZjNm&tk=tt_chain_token&video_id=v0f044gc0000cqa56uvog65s8l5g5l3g", UriKind.Absolute)
                            }
                        }
                    }
                }
            }
        };
    }
}
