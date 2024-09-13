using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using DotNext;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Common;
using Squidlr.LinkedIn.Utilities;

namespace Squidlr.LinkedIn;

public sealed partial class LinkedInContentProvider : IContentProvider
{
    private readonly LinkedInWebClient _client;
    private readonly ILogger<LinkedInContentProvider> _logger;

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.LinkedId;

    public LinkedInContentProvider(LinkedInWebClient client, ILogger<LinkedInContentProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        var identifier = UrlUtilities.GetLinkedInIdentifier(url);
        using var response = await _client.GetLinkedInPostAsync(identifier, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {LinkedInHttpStatusCode} HTTP status code when trying to request LinkedIn post.",
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

        var htmlDoc = new HtmlDocument();
        htmlDoc.Load(streamCopy);

        var scriptNode = htmlDoc.DocumentNode.Descendants("script")
                                             .FirstOrDefault(n => n.HasAttributes && n.Attributes.Any(a => a.Name == "type" && a.Value == "application/ld+json"));
        if (scriptNode == null)
        {
            _logger.LogWarning("The expected script node was not found.");
            return new(RequestContentResult.NoVideo);
        }

        using var document = JsonDocument.Parse(scriptNode.InnerHtml);
        var root = document.RootElement;

        var contentUrl = root.GetPropertyOrNull("contentUrl");
        if (contentUrl == null)
        {
            _logger.LogWarning("The expected contentUrl property was not found.");
            return new(RequestContentResult.NoVideo);
        }

        var content = new LinkedInContent(identifier.Url);
        var video = new Video();
        video.VideoSources.Add(new()
        {
            Bitrate = 0,
            ContentType = "video/mp4",
            Size = VideoSize.Empty,
            Url = new Uri(contentUrl.Value.GetString()!, UriKind.Absolute)
        });
        content.AddVideo(video);

        var author = root.GetProperty("author");
        content.UserName = author.GetProperty("name").GetString();
        content.FullText = root.GetProperty("description").GetString();

        var reactionsNode = htmlDoc.DocumentNode.Descendants("a")
                                                .FirstOrDefault(n => n.HasAttributes && n.Attributes.Any(a => a.Name == "data-num-reactions"));
        if (reactionsNode != null)
        {
            content.FavoriteCount = reactionsNode.GetAttributeValue("data-num-reactions", 0);
        }

        var datePublished = root.GetProperty("datePublished").GetDateTimeOffset();
        content.CreatedAtUtc = datePublished;

        var commentsNode = htmlDoc.DocumentNode.Descendants("a")
                                               .FirstOrDefault(n => n.HasAttributes && n.Attributes.Any(a => a.Name == "data-num-comments"));
        if (commentsNode != null)
        {
            content.ReplyCount = commentsNode.GetAttributeValue("data-num-comments", 0);
        }

        return content;
    }
}
