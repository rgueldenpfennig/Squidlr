using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using DotNext;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Common;
using Squidlr.Instagram.Utilities;

namespace Squidlr.Instagram;

public sealed class InstagramContentProvider : IContentProvider
{
    private readonly InstagramWebClient _client;
    private readonly ILogger<InstagramContentProvider> _logger;

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Instagram;

    public InstagramContentProvider(InstagramWebClient client, ILogger<InstagramContentProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        var identifier = UrlUtilities.GetInstagramIdentifier(url);

        using var response = await _client.GetAsync(
            $"/graphql/query/?query_hash=9f8827793ef34641b2fb195d4d41151c&variables={UrlEncoder.Default.Encode($$"""{"shortcode":"{{identifier.Id}}"}""")}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {InstagramHttpStatusCode} HTTP status code when trying to request Instagram post.",
                response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new(RequestContentResult.NotFound);

            return new(RequestContentResult.GatewayError);
        }

        var streamCopy = new MemoryStream(
            response.Content.Headers.ContentLength != null ?
            (int)response.Content.Headers.ContentLength : 32_768);

        await response.Content.CopyToAsync(streamCopy, cancellationToken);
        streamCopy.Position = 0;

        using var document = JsonDocument.Parse(streamCopy);
        var root = document.RootElement;

        var shortcodeMedia = root.GetPropertyOrNull("data")?
                                 .GetPropertyOrNull("shortcode_media");

        var shortcode = shortcodeMedia?.GetPropertyOrNull("shortcode")?.GetString();
        if (shortcode == null || !shortcode.Equals(identifier.Id, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("The expected shortcode {InstagramShortCode} was not found.", identifier.Id);
            return new(RequestContentResult.NotFound);
        }

        var videoUrl = shortcodeMedia!.Value.GetPropertyOrNull("video_url")?.GetString();
        if (videoUrl == null)
            return new(RequestContentResult.NoVideo);

        var content = new InstagramContent(identifier.Url);

        var owner = shortcodeMedia!.Value.GetProperty("owner");
        content.UserName = owner.GetProperty("username").GetString();

        var likes = shortcodeMedia!.Value.GetProperty("edge_media_preview_like").GetProperty("count").GetInt32();
        content.FavoriteCount = likes;

        var takenAtTimestamp = shortcodeMedia!.Value.GetProperty("taken_at_timestamp").GetInt64();
        content.CreatedAtUtc = DateTimeOffset.FromUnixTimeSeconds(takenAtTimestamp);

        var edgeMediaToCaption = shortcodeMedia!.Value.GetProperty("edge_media_to_caption");
        foreach (var edge in edgeMediaToCaption.GetProperty("edges").EnumerateArray())
        {
            var node = edge.GetPropertyOrNull("node");
            if (node != null)
            {
                content.FullText = node!.Value.GetProperty("text").GetString();
                break;
            }
        }

        var edgeMediaToComment = shortcodeMedia!.Value.GetProperty("edge_media_to_comment");
        content.ReplyCount = edgeMediaToComment.GetProperty("count").GetInt32();

        var video = new InstagramVideo()
        {
            DisplayUrl = new Uri(shortcodeMedia!.Value.GetProperty("display_url").GetString()!, UriKind.Absolute),
            Duration = TimeSpan.FromSeconds(shortcodeMedia!.Value.GetProperty("video_duration").GetDouble())
        };

        var videoUri = new Uri(videoUrl, UriKind.Absolute);
        var contentLength = await _client.GetVideoContentLengthAsync(videoUri, cancellationToken);

        video.VideoSources.Add(new()
        {
            Bitrate = 0,
            ContentLength = contentLength,
            ContentType = "video/mp4",
            Size = VideoSize.Empty,
            Url = videoUri
        });

        content.Videos.Add(video);

        return content;
    }
}
