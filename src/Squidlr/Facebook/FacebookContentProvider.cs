using System.Net;
using System.Text.RegularExpressions;
using DotNext;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Facebook.Utilities;

namespace Squidlr.Facebook;

public sealed partial class FacebookContentProvider : IContentProvider
{
    private readonly FacebookWebClient _client;
    private readonly ILogger<FacebookContentProvider> _logger;

    [GeneratedRegex(@"""unified_reactors"":{""count"":(?<count>\d+)}", RegexOptions.IgnoreCase)]
    private static partial Regex UnifiedReactorsCountRegex();

    [GeneratedRegex(@"""total_comment_count"":(?<count>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TotalCommentCountRegex();

    [GeneratedRegex(@"""share_count_reduced"":""(?<count>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex ShareCountRegex();

    [GeneratedRegex(@"""browser_native_hd_url"":""(?<url>[^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex NativeHdUrlRegex();

    [GeneratedRegex(@"""playback_video"":{""height"":(?<height>\d+),""width"":(?<width>\d+),""length_in_second"":(?<seconds>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PlaybackVideoRegex();

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Facebook;

    public FacebookContentProvider(FacebookWebClient client, ILogger<FacebookContentProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        var identifier = UrlUtilities.GetFacebookIdentifier(url);
        using var response = await _client.GetFacebookPostAsync(identifier, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {FacebookHttpStatusCode} HTTP status code when trying to request Facebook post.",
                response.StatusCode);

            return response.StatusCode == HttpStatusCode.NotFound ? new(RequestContentResult.NotFound) : new(RequestContentResult.GatewayError);
        }

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var videoUrlMatch = NativeHdUrlRegex().Match(htmlContent);
        if (!videoUrlMatch.Success)
        {
            _logger.LogWarning("The expected video URL was not found.");
            return new(RequestContentResult.NoVideo);
        }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        var ogImage = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
        var videoDisplayUrl = ogImage?.GetAttributeValue("content", null);

        var videoUrl = videoUrlMatch.Groups["url"].Value.Replace("\\", string.Empty);

        var content = new FacebookContent(identifier.Url);
        var videoFileUrl = new Uri(videoUrl, UriKind.Absolute);
        var video = new Video
        {
            DisplayUrl = videoDisplayUrl != null ? new Uri(videoDisplayUrl, UriKind.Absolute) : null
        };

        var (contentLength, mediaType) = await _client.GetVideoContentLengthAndMediaTypeAsync(videoFileUrl, cancellationToken);

        video.VideoSources.Add(new()
        {
            Bitrate = 0,
            ContentLength = contentLength,
            ContentType = mediaType ?? "video/mp4",
            Size = VideoSize.Empty,
            Url = videoFileUrl
        });
        content.AddVideo(video);

        return content;
    }
}
