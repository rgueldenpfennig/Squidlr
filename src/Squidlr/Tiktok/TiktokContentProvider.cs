using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNext;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Common;
using Squidlr.Tiktok.Utilities;

namespace Squidlr.Tiktok;

public sealed partial class TiktokContentProvider : IContentProvider
{
    [GeneratedRegex(
        @"<script id=""__UNIVERSAL_DATA_FOR_REHYDRATION__"".*type=""application\/json"">(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex UniversalDataRegex();

    private readonly TiktokWebClient _client;
    private readonly ILogger<TiktokContentProvider> _logger;

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Tiktok;

    public TiktokContentProvider(TiktokWebClient client, ILogger<TiktokContentProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        var identifier = UrlUtilities.GetTiktokIdentifier(url);
        using var response = await _client.GetTiktokPostAsync(identifier, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {TiktokHttpStatusCode} HTTP status code when trying to request Tiktok post.",
                response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new(RequestContentResult.NotFound);

            return new(RequestContentResult.GatewayError);
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var match = UniversalDataRegex().Match(html);
        if (!match.Success)
        {
            _logger.LogError("Could not find universal data script part in HTML document.");
            return new(RequestContentResult.Error);
        }

        var json = match.Groups["json"].Value;

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var videoDetail = root.GetPropertyOrNull("__DEFAULT_SCOPE__")?.GetPropertyOrNull("webapp.video-detail");
        if (videoDetail == null)
        {
            _logger.LogWarning("Could not find 'webapp.video-detail' in JSON payload.");
            return new(RequestContentResult.NoVideo);
        }

        var statusCode = videoDetail.Value.GetPropertyOrNull("statusCode")?.GetInt32();
        if (statusCode > 0)
        {
            return statusCode switch
            {
                10204 => new(RequestContentResult.NotFound),
                10216 => new(RequestContentResult.Protected),
                _ => new(RequestContentResult.GatewayError)
            };
        }

        var itemStruct = videoDetail.Value.GetProperty("itemInfo").GetProperty("itemStruct");
        var id = itemStruct.GetProperty("id").GetString();
        if (id == null || !id.Equals(identifier.Id, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("ID does not match requested Tiktok identifier.");
        }

        if (itemStruct.GetPropertyOrNull("isContentClassified")?.GetBoolean() == true)
        {
            return new(RequestContentResult.AdultContent);
        }

        if (itemStruct.GetPropertyOrNull("imagePost") != null)
        {
            _logger.LogWarning("Tiktok image posts are not supported yet.");
            return new(RequestContentResult.NoVideo);
        }

        var content = new TiktokContent(identifier.Url);
        content.CreatedAtUtc = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(itemStruct.GetProperty("createTime").GetString(), CultureInfo.InvariantCulture));
        content.FullText = itemStruct.GetProperty("desc").GetString();
        content.Username = itemStruct.GetProperty("author").GetProperty("uniqueId").GetString();

        var stats = itemStruct.GetProperty("stats");
        content.FavoriteCount = stats.GetProperty("diggCount").GetInt32();
        content.ReplyCount = stats.GetProperty("commentCount").GetInt32();
        content.PlayCount = stats.GetProperty("playCount").GetInt32();
        content.ShareCount = stats.GetProperty("shareCount").GetInt32();
        content.CollectCount = Convert.ToInt32(stats.GetProperty("collectCount").GetString(), CultureInfo.InvariantCulture);

        var video = new Video();
        var videoElement = itemStruct.GetProperty("video");
        video.DisplayUrl = new(videoElement.GetProperty("cover").GetString()!, UriKind.Absolute);
        video.Duration = TimeSpan.FromSeconds(videoElement.GetProperty("duration").GetInt32());

        var height = videoElement.GetProperty("height").GetInt32();
        var width = videoElement.GetProperty("width").GetInt32();

        foreach (var bitrateInfo in videoElement.GetProperty("bitrateInfo").EnumerateArray())
        {
            var videoSource = new VideoSource
            {
                Bitrate = bitrateInfo.GetProperty("Bitrate").GetInt32(),
                Size = new VideoSize(height, width),
                ContentType = "video/mp4",
                Url = new Uri(bitrateInfo.GetProperty("PlayAddr").GetProperty("UrlList").EnumerateArray().First().GetString()!, UriKind.Absolute),
                ContentLength = Convert.ToInt32(bitrateInfo.GetProperty("PlayAddr").GetProperty("DataSize").GetString(), CultureInfo.InvariantCulture)
            };

            video.VideoSources.Add(videoSource);
        }

        content.Videos.Add(video);

        // provide TikTok cookies
        foreach (var cookie in response.Headers.GetValues("Set-Cookie"))
        {
            content.AdditionalProperties.Add(cookie[..cookie.IndexOf('=')], cookie);
        }

        return content;
    }
}
