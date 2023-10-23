using System.Net;
using Squidlr.Twitter;
using Xunit.Abstractions;

namespace Squidlr.Api.IntegrationTests.Content;

public class ContentRouteTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContentRouteTests(ApiWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        factory.TestOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-KEY", "foobar");
    }

    [Theory]
    // Amplify video card
    [InlineData("https://twitter.com/starwars/status/665052190608723968")]
    // embedded video from other source than Twitter
    [InlineData("https://twitter.com/Filmdrunk/status/713801302971588609", HttpStatusCode.NotFound)]
    // Broadcast - not supported yet
    [InlineData("https://twitter.com/ViviEducation/status/1136534865145286656", HttpStatusCode.NotFound)]
    [InlineData("https://twitter.com/OPP_HSD/status/779210622571536384", HttpStatusCode.NotFound)]
    [InlineData("https://twitter.com/LouisVuitton/status/1671250425813815296", HttpStatusCode.NotFound)]
    // Tweet with adult content
    [InlineData("https://twitter.com/Rizdraws/status/1575199173472927762", HttpStatusCode.UnavailableForLegalReasons)]
    // Video Tweet including other quoted video
    [InlineData("https://twitter.com/DavidToons_/status/1578353380363501568")]
    // Twitter space recording
    [InlineData("https://twitter.com/MoniqueCamarra/status/1550101959377551360", HttpStatusCode.NotFound)]
    // Tweet from suspended account
    [InlineData("https://twitter.com/GunB1g/status/1163218564784017422", HttpStatusCode.NotFound)]
    // unified_card with 2 embedded Twitter videos
    [InlineData("https://twitter.com/primevideouk/status/1578401165338976258")]
    // unified_card with 3 embedded Twitter videos
    [InlineData("https://twitter.com/nexta_tv/status/1675496442109018112?s=20")]
    // unified_card with embedded Twitter advertising video for a mobile app
    [InlineData("https://twitter.com/poco_dandy/status/1150646424461176832")]
    // promo_video_convo card
    [InlineData("https://twitter.com/poco_dandy/status/1047395834013384704")]
    // video_direct_message card
    [InlineData("https://twitter.com/qarev001/status/1348948114569269251")]
    // GIF
    [InlineData("https://twitter.com/starwars/status/1654199211410477056")]
    // quoted Tweet with video
    [InlineData("https://twitter.com/Markus_Krall/status/1679960529484259329")]
    [InlineData("https://twitter.com/Srirachachau/status/1395079556562706435")]
    // default video Tweets
    [InlineData("https://twitter.com/CAF_Online/status/1349365911120195585")]
    [InlineData("https://twitter.com/SamsungMobileSA/status/1348609186725289984")]
    [InlineData("https://twitter.com/SouthamptonFC/status/1347577658079641604")]
    [InlineData("https://twitter.com/CTVJLaidlaw/status/1600649710662213632")]
    [InlineData("https://twitter.com/s2FAKER/status/1621117700482416640")]
    [InlineData("https://twitter.com/hlo_again/status/1599108751385972737")]
    [InlineData("https://twitter.com/MunTheShinobi/status/1600009574919962625")]
    [InlineData("https://twitter.com/liberdalau/status/1623739803874349067", Skip = "Account owner limits who can view their Tweets")]
    [InlineData("https://twitter.com/oshtru/status/1577855540407197696")]
    [InlineData("https://twitter.com/UltimaShadowX/status/1577719286659006464")]
    [InlineData("https://twitter.com/MesoMax919/status/1575560063510810624")]
    [InlineData("https://twitter.com/captainamerica/status/719944021058060289")]
    [InlineData("https://twitter.com/news_al3alm/status/852138619213144067")]
    [InlineData("https://twitter.com/LisPower1/status/1001551623938805763")]
    [InlineData("https://twitter.com/jaydingeer/status/700207533655363584")]
    [InlineData("https://twitter.com/freethenipple/status/643211948184596480")]
    public async Task RequestTweetContent(string tweetUrl, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        // Arrange & Act
        var response = await _client.GetAsync($"/content?url={tweetUrl}");

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<TwitterContent>();
            Assert.NotNull(content);
        }
    }
}
