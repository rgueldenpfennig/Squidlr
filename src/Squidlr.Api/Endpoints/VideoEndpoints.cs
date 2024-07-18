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

    public static RouteHandlerBuilder MapVideoEndpoints(this IEndpointRouteBuilder builder)
    {
        return builder.AddStreamVideo();
    }

    private static RouteHandlerBuilder AddStreamVideo(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/video", StreamVideoAsync)
            .WithName("StreamVideo")
            .Produces<FileContentResult>(StatusCodes.Status200OK, contentType: "video/mp4")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, contentType: "application/json")
            .AllowAnonymous();
    }

    private static async ValueTask<IResult> StreamVideoAsync(
        string contentUrl,
        [FromServices] ContentProvider contentProvider,
        [FromServices] UrlResolver urlResolver,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] HttpFileStreamService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(contentUrl))
        {
            return Results.Problem(_badRequestDetails);
        }

        // TODO: ensure that the video URL is valid in conjunction with the content platform

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

        var content = await contentProvider.GetContentAsync(contentIdentifier, cancellationToken);
        var cookie = content.Value.AdditionalProperties["tt_chain_token"];

        // TODO: provide parameter to look for desired video quality

        var videoUri = content.Value.Videos.First().VideoSources.First().Url;
        var request = new HttpRequestMessage(HttpMethod.Get, videoUri);
        request.Headers.Host = videoUri.Host;
        request.Headers.Add(HeaderNames.Cookie, cookie);

        // sets the file name for the file download
        context.Response.Headers.ContentDisposition = $"attachment; fileName={contentIdentifier.Platform}-Squidlr-{contentIdentifier.Id}.mp4";

        var httpClient = httpClientFactory.CreateClient(httpClientName);
        await service.CopyFileStreamAsync(context, httpClient, request, cancellationToken);

        return Results.Empty;
    }
}