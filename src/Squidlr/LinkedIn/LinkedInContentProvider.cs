﻿using System.Net;
using System.Text.Json;
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

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.LinkedIn;

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

            return response.StatusCode == HttpStatusCode.NotFound ? new(RequestContentResult.NotFound) : new(RequestContentResult.GatewayError);
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
        var videoFileUrl = new Uri(contentUrl.Value.GetString()!, UriKind.Absolute);
        var video = new Video
        {
            DisplayUrl = new Uri(root.GetProperty("thumbnailUrl").GetString()!, UriKind.Absolute)
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
        content.FullText = root.GetPropertyOrNull("description")?.GetString();

        var creator = root.GetPropertyOrNull("creator");
        if (creator != null)
        {
            content.Username = creator.Value.GetPropertyOrNull("name")?.GetString();
            content.UserUrl = creator.Value.GetPropertyOrNull("url")?.GetString();
        }
        else
        {
            _logger.LogWarning("Could not obtain the LinkedIn creator property.");
        }

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
