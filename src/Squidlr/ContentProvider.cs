﻿using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Telemetry;

namespace Squidlr;

public sealed class ContentProvider
{
    private readonly IReadOnlyList<IContentProvider> _contentProviders;

    private readonly IMemoryCache _memoryCache;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ContentProvider> _logger;

    private static readonly Result<Content, RequestContentResult> _platformNotSupportedResult = new (RequestContentResult.PlatformNotSupported);

    public ContentProvider(
        IReadOnlyList<IContentProvider> contentProviders,
        IMemoryCache memoryCache,
        ITelemetryService telemetryService,
        ILogger<ContentProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(contentProviders);
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(logger);
        if (contentProviders.Count == 0)
        {
            throw new ArgumentException($"No {nameof(IContentProvider)} instances have been provided.", nameof(contentProviders));
        }

        _contentProviders = contentProviders;
        _memoryCache = memoryCache;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(ContentIdentifier contentIdentifier, CancellationToken cancellationToken)
    {
        var eventProperties = new Dictionary<string, string>
        {
            { "ContentId", contentIdentifier.Id },
            { "Url", contentIdentifier.Url },
            { "SocialMediaPlatform", contentIdentifier.Platform.ToString() }
        };

        _telemetryService.TrackEvent("ContentRequested", eventProperties);

        var cacheKey = $"{contentIdentifier.Platform}-{contentIdentifier.Id}";
        if (_memoryCache.TryGetValue<Result<Content, RequestContentResult>>(cacheKey, out var result))
        {
            _logger.LogInformation(
                "Cache hit for {ContentId} at {ContentUrl} on {SocialMediaPlatform}",
                contentIdentifier.Id, contentIdentifier.Url, contentIdentifier.Platform);

            eventProperties.Add("CacheHit", "true");
            if (result.Error == RequestContentResult.Success)
            {
                TrackContentRequestSucceeded(eventProperties);
            }
            else
            {
                TrackContentRequestFailed(result.Error.ToString(), eventProperties);
            }

            return result;
        }

        for (var i = 0; i < _contentProviders.Count; i++)
        {
            var provider = _contentProviders[i];
            if (provider.Platform == contentIdentifier.Platform)
            {
                try
                {
                    _logger.LogInformation(
                        "Loading content from {SocialMediaPlatform}: {ContentId} at {ContentUrl}",
                        contentIdentifier.Platform, contentIdentifier.Id, contentIdentifier.Url);

                    var content = await provider.GetContentAsync(contentIdentifier.Url, cancellationToken);
                    if (content.Error == RequestContentResult.Success)
                    {
                        TrackContentRequestSucceeded(eventProperties);
                        _memoryCache.Set(cacheKey, content, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60));
                    }
                    else
                    {
                        TrackContentRequestFailed(content.Error.ToString(), eventProperties);

                        if (ShouldBeCached(content.Error))
                        {
                            _memoryCache.Set(cacheKey, content, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60));
                        }
                    }

                    return content;
                }
                catch (OperationCanceledException)
                {
                    TrackContentRequestFailed(RequestContentResult.Canceled.ToString(), eventProperties);
                    return new(RequestContentResult.Canceled);
                }
                catch (Exception e)
                {
                    TrackContentRequestFailed(e.Message, eventProperties);
                    _logger.LogError(e, "An unexpected error occurred while using the {SocialMediaPlatform} content provider.", provider.Platform);

                    return new(RequestContentResult.Error);
                }
            }
        }

        TrackContentRequestFailed(_platformNotSupportedResult.Error.ToString(), eventProperties);
        return _platformNotSupportedResult;
    }

    private void TrackContentRequestSucceeded(Dictionary<string, string> eventProperties)
    {
        _telemetryService.TrackEvent("ContentRequestSucceeded", eventProperties);
    }

    private void TrackContentRequestFailed(string reason, Dictionary<string, string> eventProperties)
    {
        eventProperties.Add("Reason", reason);
        _telemetryService.TrackEvent("ContentRequestFailed", eventProperties);
    }

    private static bool ShouldBeCached(RequestContentResult error)
    {
        return error is RequestContentResult.NotFound
                     or RequestContentResult.PlatformNotSupported
                     or RequestContentResult.NoVideo
                     or RequestContentResult.UnsupportedVideo
                     or RequestContentResult.AccountSuspended
                     or RequestContentResult.Protected
                     or RequestContentResult.AdultContent;
    }
}
