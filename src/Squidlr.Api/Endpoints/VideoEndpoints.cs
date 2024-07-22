using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Squidlr.Hosting;
using Squidlr.Tiktok;

namespace Squidlr.Api.Endpoints;

internal static class VideoEndpoints
{
    private static readonly ProblemDetails _badRequestDetails = new()
    {
        Status = StatusCodes.Status400BadRequest
    };

    public static RouteHandlerBuilder MapVideoEndpoints(this IEndpointRouteBuilder builder, IWebHostEnvironment environment)
    {
        return builder.AddStreamVideo(environment);
    }

    private static RouteHandlerBuilder AddStreamVideo(this IEndpointRouteBuilder builder, IWebHostEnvironment environment)
    {
        var handler = builder.MapGet("/video", StreamVideoAsync)
            .WithName("StreamVideo")
            .Produces<FileContentResult>(StatusCodes.Status200OK, contentType: "video/mp4")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, contentType: "application/json")
            .AllowAnonymous();

        if (!environment.IsDevelopment())
        {
            handler.RequireRateLimiting("Video");
        }

        return handler;
    }

    private static async ValueTask<IResult> StreamVideoAsync(
        string contentUrl,
        string videoSelector,
        [FromServices] ContentProvider contentProvider,
        [FromServices] UrlResolver urlResolver,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] HttpFileStreamService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(contentUrl) || string.IsNullOrEmpty(videoSelector))
        {
            return Results.Problem(_badRequestDetails);
        }

        var contentIdentifier = urlResolver.ResolveUrl(contentUrl);
        var httpClientName = contentIdentifier.Platform switch
        {
            SocialMediaPlatform.Tiktok => TiktokWebClient.HttpClientName,
            _ => null
        };

        if (httpClientName is null)
        {
            return Results.Problem(_badRequestDetails);
        }

        // TODO: support for other platforms than TikTok
        var content = await contentProvider.GetContentAsync(contentIdentifier, cancellationToken);
        if (!content.IsSuccessful)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (!content.Value.AdditionalProperties.TryGetValue("tt_chain_token", out var ttChainTokenCookie))
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        var videoUri = content.Value.Videos.SelectMany(v => v.VideoSources)
                                           .FirstOrDefault(vs => vs.Url.PathAndQuery
                                           .Contains(videoSelector, StringComparison.OrdinalIgnoreCase))?.Url;

        if (videoUri == null)
        {
            return Results.NotFound();
        }

        var videoRequest = new HttpRequestMessage(HttpMethod.Get, videoUri);
        videoRequest.Headers.Host = videoUri.Host;
        videoRequest.Headers.Add(HeaderNames.Cookie, ttChainTokenCookie);

        // sets the file name for the file download
        context.Response.Headers.ContentDisposition = $"attachment; fileName={contentIdentifier.Platform}-Squidlr-{contentIdentifier.Id}.mp4";

        var httpClient = httpClientFactory.CreateClient(httpClientName);
        await service.CopyFileStreamAsync(context, httpClient, videoRequest, cancellationToken);

        return Results.Empty;
    }
}