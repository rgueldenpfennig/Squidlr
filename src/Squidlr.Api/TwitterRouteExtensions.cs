using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Squidlr.Twitter;
using Squidlr.Twitter.Services;
using Squidlr.Utilities;

namespace Squidlr.Api;

internal static class TwitterRouteExtensions
{
    public static RouteHandlerBuilder MapTwitterRoutes(this IEndpointRouteBuilder builder)
    {
        builder.AddGetContent();
        return builder.AddGetVideo();
    }

    private static RouteHandlerBuilder AddGetContent(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/content", async (string url, [FromServices] ITweetContentService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!UrlUtilities.IsValidTwitterStatusUrl(url))
            {
                return Results.Problem(new()
                {
                    Detail = "The given URL seems not be a valid Twitter status URL.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var tweetIdentifier = UrlUtilities.CreateTweetIdentifierFromUrl(url);

            using (LogContext.PushProperty("TweetId", tweetIdentifier.Id))
            {
                var result = await service.GetTweetContentAsync(tweetIdentifier, cancellationToken);

                if (!result.IsSuccessful)
                {

                    return CreateProblemResult(result.Error);
                }

                var tweetContent = result.Value;
                return Results.Ok(tweetContent);
            }
        })
        .WithName("GetContent")
        .WithOpenApi(operation =>
        {
            var parameter = operation.Parameters[0];
            parameter.Description = "The Twitter URL that identifies a Tweet with a video.";

            operation.Summary = "Provides the content details of the requested Tweet URL.";
            operation.Description = "If a valid Tweet URL is provided this API will respond with the details containing all video variants.";

            return operation;
        })
        .ProducesValidationProblem()
        .Produces<TweetContent>()
        .RequireAuthorization();
    }

    private static RouteHandlerBuilder AddGetVideo(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/video", async (string tweetId, string url, [FromServices] TweetMediaService service, HttpContext context, CancellationToken cancellationToken) =>
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
                await service.CopyTweetVideoStreamAsync(tweetId, new Uri(url, UriKind.Absolute), context, cancellationToken);
                return Results.Empty;
            }
        })
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

    private static IResult CreateProblemResult(GetTweetVideoResult result)
    {
        var reponseText = "The Tweet video could not be obtained.";
        var responseCode = StatusCodes.Status500InternalServerError;

        switch (result)
        {
            case GetTweetVideoResult.NotFound:
                reponseText = "The Tweet was not found.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case GetTweetVideoResult.NoVideo:
                reponseText = "The Tweet contains no downloadable video.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case GetTweetVideoResult.UnsupportedVideo:
                reponseText = "The Tweet contains an embedded video source that is not yet supported.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case GetTweetVideoResult.AccountSuspended:
                reponseText = "The account containing the requested Tweet has been suspended.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case GetTweetVideoResult.Protected:
                reponseText = "The account owner limits who can view their Posts.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case GetTweetVideoResult.AdultContent:
                reponseText = "Age-restricted adult content. This content might not be appropriate for people under 18 years old.";
                responseCode = StatusCodes.Status451UnavailableForLegalReasons;
                break;
            case GetTweetVideoResult.Error:
                reponseText = "An error occured while requesting the original video.";
                responseCode = StatusCodes.Status502BadGateway;
                break;
            case GetTweetVideoResult.Canceled:
                reponseText = "The processing has been cancelled.";
                responseCode = 499;
                break;
        }

        var details = new ProblemDetails
        {
            Detail = reponseText,
            Status = responseCode
        };

        details.Extensions.Add("result", result);
        return Results.Problem(details);
    }
}