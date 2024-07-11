using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Squidlr.Twitter;
using Squidlr.Twitter.Utilities;

namespace Squidlr.Api.Endpoints;

internal static class VideoEndpoints
{
    public static RouteHandlerBuilder MapVideoEndpoints(this IEndpointRouteBuilder builder)
    {
        return builder.AddGetVideo();
    }

    private static RouteHandlerBuilder AddGetVideo(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/video", GetVideoAsync)
            .WithName("GetVideo")
            .WithOpenApi(operation =>
            {
                var parameter = operation.Parameters[0];
                parameter.Description = "The related Tweet ID for the video file.";

                parameter = operation.Parameters[1];
                parameter.Description = "The Twitter URL to a video file.";

                operation.Summary = "Provides the video file stream of the requested Twitter video URL.";

                return operation;
            })
            .ProducesValidationProblem()
            .Produces<FileContentResult>(StatusCodes.Status200OK, contentType: "video/mp4")
            .RequireRateLimiting("Video")
            .RequireAuthorization();
    }

    private static async ValueTask<IResult> GetVideoAsync(
        string tweetId,
        string url,
        [FromServices] HttpFileStreamService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(tweetId))
            return Results.Problem(new()
            {
                Detail = "TweetId must be set.",
                Status = StatusCodes.Status400BadRequest
            });

        if (!UrlUtilities.IsValidTwitterVideoUrl(url))
        {
            return Results.Problem(new()
            {
                Detail = "The given URL seems not be a valid Twitter video URL.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        using (LogContext.PushProperty("TweetId", tweetId))
        {
            await service.CopyFileStreamAsync(context, TwitterWebClient.HttpClientName, tweetId, new Uri(url, UriKind.Absolute), cancellationToken);
            return Results.Empty;
        }
    }
}