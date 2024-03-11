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
            _logger.LogError("Could not find 'webapp.video-detail' in JSON payload.");
            return new(RequestContentResult.Error);
        }

        var itemStruct = videoDetail.Value.GetProperty("itemInfo").GetProperty("itemStruct");
        var id = itemStruct.GetProperty("id").GetString();
        if (id == null || !id.Equals(identifier.Id, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("ID does not match requested Tiktok identifier.");
            return new(RequestContentResult.NotFound);
        }

        var content = new TiktokContent(identifier.Url);
        content.CreatedAtUtc = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(itemStruct.GetProperty("createTime").GetString()));
        content.FullText = itemStruct.GetProperty("desc").GetString();
        content.UserName = itemStruct.GetProperty("author").GetProperty("uniqueId").GetString();

        var stats = itemStruct.GetProperty("stats");
        content.FavoriteCount = stats.GetProperty("diggCount").GetInt32();
        content.ReplyCount = stats.GetProperty("commentCount").GetInt32();
        content.PlayCount = stats.GetProperty("playCount").GetInt32();
        content.CollectCount = Convert.ToInt32(stats.GetProperty("collectCount").GetString());

        var video = new TiktokVideo();
        var videoElement = itemStruct.GetProperty("video");
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
                Url = new Uri(bitrateInfo.GetProperty("PlayAddr").GetProperty("UrlList").EnumerateArray().Last().GetString()!, UriKind.Absolute),
                ContentLength = Convert.ToInt32(bitrateInfo.GetProperty("PlayAddr").GetProperty("DataSize").GetString())
            };

            video.VideoSources.Add(videoSource);
        }

        content.Videos.Add(video);
        return content;
    }
}
