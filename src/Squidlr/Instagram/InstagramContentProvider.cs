using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNext;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Common;
using Squidlr.Instagram.Utilities;

namespace Squidlr.Instagram;

public sealed partial class InstagramContentProvider : IContentProvider
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
        using var response = await _client.GetInstagramPostAsync(identifier, cancellationToken);

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
            var restrictionResult = await HasRestrictedProfileAsync(identifier, cancellationToken);
            if (restrictionResult.HasValue)
            {
                return new(restrictionResult.Value);
            }

            _logger.LogWarning("The expected shortcode {InstagramShortCode} was not found.", identifier.Id);
            return new(RequestContentResult.NotFound);
        }

        var content = new InstagramContent(identifier.Url);

        var isVideo = shortcodeMedia!.Value.GetProperty("is_video").GetBoolean();
        if (isVideo)
        {
            // this is a single video post
            var video = await ExtractVideoFromNode(shortcodeMedia.Value, cancellationToken);
            content.Videos.Add(video);
        }

        var edgeSidecarToChildren = shortcodeMedia.Value.GetPropertyOrNull("edge_sidecar_to_children");
        if (edgeSidecarToChildren != null)
        {
            // lets check if the post contains multiple videos
            foreach (var edge in edgeSidecarToChildren.Value.GetProperty("edges").EnumerateArray())
            {
                var node = edge.GetPropertyOrNull("node");
                if (node != null && node.Value.GetPropertyOrNull("is_video")?.GetBoolean() == true)
                {
                    var video = await ExtractVideoFromNode(node.Value, cancellationToken);
                    content.Videos.Add(video);
                }
            }
        }

        if (content.Videos.Count == 0)
        {
            return new(RequestContentResult.NoVideo);
        }

        var owner = shortcodeMedia.Value.GetProperty("owner");
        content.Username = owner.GetProperty("username").GetString();
        content.FullName = owner.GetProperty("full_name").GetString();
        if (owner.TryGetProperty("profile_pic_url", out var profilePic))
        {
            content.ProfilePictureUrl = new(profilePic.GetString()!, UriKind.Absolute);
        }

        var likes = shortcodeMedia.Value.GetProperty("edge_media_preview_like").GetProperty("count").GetInt32();
        content.FavoriteCount = likes;

        var takenAtTimestamp = shortcodeMedia.Value.GetProperty("taken_at_timestamp").GetInt64();
        content.CreatedAtUtc = DateTimeOffset.FromUnixTimeSeconds(takenAtTimestamp);

        var edgeMediaToCaption = shortcodeMedia.Value.GetProperty("edge_media_to_caption");
        foreach (var edge in edgeMediaToCaption.GetProperty("edges").EnumerateArray())
        {
            var node = edge.GetPropertyOrNull("node");
            if (node != null)
            {
                content.FullText = node!.Value.GetProperty("text").GetString();
                break;
            }
        }

        var edgeMediaToComment = shortcodeMedia.Value.GetProperty("edge_media_to_comment");
        content.ReplyCount = edgeMediaToComment.GetProperty("count").GetInt32();

        return content;
    }

    private async ValueTask<Video> ExtractVideoFromNode(JsonElement videoNode, CancellationToken cancellationToken)
    {
        var videoUrl = videoNode.GetProperty("video_url").GetString()!;
        var video = new Video()
        {
            DisplayUrl = new Uri(videoNode.GetProperty("display_url").GetString()!, UriKind.Absolute),
            Duration = videoNode.TryGetProperty("video_duration", out var videoDuration) ? TimeSpan.FromSeconds(videoDuration.GetDouble()) : null,
            Views = videoNode.GetProperty("video_view_count").GetInt32()
        };

        var videoUri = new Uri(videoUrl, UriKind.Absolute);
        var (contentLength, contentType) = await _client.GetVideoContentLengthAndMediaTypeAsync(videoUri, cancellationToken);
        var dimensions = videoNode.GetProperty("dimensions");
        var videoSize = new VideoSize(
            dimensions.GetProperty("height").GetInt32(),
            dimensions.GetProperty("width").GetInt32());

        video.VideoSources.Add(new()
        {
            Bitrate = 0,
            ContentLength = contentLength,
            ContentType = contentType ?? "video/mp4",
            Size = videoSize,
            Url = videoUri
        });

        return video;
    }

    [GeneratedRegex(@"""media_id"":""(?<mediaId>\d+)"",""media_owner_id"":""(?<mediaOwnerId>\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex MediaOwnerIdRegex();

    private async ValueTask<RequestContentResult?> HasRestrictedProfileAsync(InstagramIdentifier identifier, CancellationToken cancellationToken)
    {
        using var response = await _client.GetInstagramResponseAsync(identifier.Url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {InstagramHttpStatusCode} HTTP status code when trying to request Instagram raw post.",
                response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return RequestContentResult.NotFound;

            return RequestContentResult.GatewayError;
        }

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var match = MediaOwnerIdRegex().Match(htmlContent);
        if (!match.Success)
            return null;

        var mediaId = match.Groups["mediaId"].Value;
        var mediaOwnerId = match.Groups["mediaOwnerId"].Value;

        using var rulingResponse = await _client.GetInstagramResponseAsync(
            $"/api/v1/web/get_ruling_for_media_content_logged_out/?media_id={mediaId}&owner_id={mediaOwnerId}",
            cancellationToken);

        if (!rulingResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {InstagramHttpStatusCode} HTTP status code when trying to request Instagram ruling content.",
                rulingResponse.StatusCode);

            if (rulingResponse.StatusCode == HttpStatusCode.NotFound)
                return RequestContentResult.NotFound;

            return RequestContentResult.GatewayError;
        }

        htmlContent = await rulingResponse.Content.ReadAsStringAsync(cancellationToken);
        if (htmlContent.Contains("18 years", StringComparison.OrdinalIgnoreCase))
        {
            return RequestContentResult.AdultContent;
        }

        return RequestContentResult.Protected;
    }
}
