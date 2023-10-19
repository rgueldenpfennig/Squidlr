using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Squidlr.Twitter;
using Squidlr.Twitter.Services;
using Squidlr.Twitter.Utilities;

namespace Squidlr.Api;

internal static class ContentRouteExtensions
{
    public static RouteHandlerBuilder MapContentRoutes(this IEndpointRouteBuilder builder)
    {
        return builder.AddGetContent();
    }

    private static RouteHandlerBuilder AddGetContent(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/content",
            async (
                string url,
                [FromServices] UrlResolver urlResolver,
                [FromServices] ITweetContentService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
        {
            if (urlResolver.ResolveUrl(url) == SocialMediaPlatform.Unknown)
            {
                return CreateProblemResult(RequestContentResult.PlatformNotSupported);
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
            parameter.Description = "The content URL of a social media platform.";

            operation.Summary = "Provides the content details of the requested content URL.";
            operation.Description = "If a valid Tweet URL is provided this API will respond with the details containing all video variants.";

            return operation;
        })
        .ProducesValidationProblem()
        .Produces<TwitterContent>()
        .RequireAuthorization();
    }

    private static IResult CreateProblemResult(RequestContentResult result)
    {
        var reponseText = "The video could not be obtained.";
        var responseCode = StatusCodes.Status500InternalServerError;

        switch (result)
        {
            case RequestContentResult.NotFound:
                reponseText = "The content was not found.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case RequestContentResult.PlatformNotSupported:
                reponseText = "The requested content seems to belong to an unsupported social media platform.";
                responseCode = StatusCodes.Status400BadRequest;
                break;
            case RequestContentResult.NoVideo:
                reponseText = "The content contains no downloadable video.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case RequestContentResult.UnsupportedVideo:
                reponseText = "The content contains an embedded video source that is not yet supported.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case RequestContentResult.AccountSuspended:
                reponseText = "The account containing the requested content has been suspended.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case RequestContentResult.Protected:
                reponseText = "The account owner limits who can view their Posts.";
                responseCode = StatusCodes.Status404NotFound;
                break;
            case RequestContentResult.AdultContent:
                reponseText = "Age-restricted adult content. This content might not be appropriate for people under 18 years old.";
                responseCode = StatusCodes.Status451UnavailableForLegalReasons;
                break;
            case RequestContentResult.Error:
                reponseText = "An error occured while requesting the original video.";
                responseCode = StatusCodes.Status502BadGateway;
                break;
            case RequestContentResult.Canceled:
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
