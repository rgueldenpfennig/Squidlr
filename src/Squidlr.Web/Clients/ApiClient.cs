using System.Web;
using DotNext;
using Microsoft.AspNetCore.Mvc;
using Squidlr.Facebook;
using Squidlr.Instagram;
using Squidlr.LinkedIn;
using Squidlr.Shared;
using Squidlr.Tiktok;
using Squidlr.Twitter;
using Squidlr.Web.States;

namespace Squidlr.Web.Clients;

public sealed class ApiClient
{
    public const string HttpClientName = nameof(ApiClient);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        _logger.LogInformation("Requesting content for '{ContentUrl}'", url);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString();

            var request = new HttpRequestMessage(HttpMethod.Get, $"/content?url={HttpUtility.UrlEncode(url)}");
            if (ipAddress != null)
            {
                request.Headers.Add("X-Forwarded-For", ipAddress);
            }

            if (context?.Items.ContainsKey(nameof(AppState)) == true)
            {
                var appState = context.Items[nameof(AppState)] as AppState;
                if (appState != null)
                {
                    request.Headers.Add(SquidlrHeaderNames.SessionId, appState.SessionId);
                    request.Headers.Add(SquidlrHeaderNames.UserId, appState.UserId);
                }
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (!response.Headers.TryGetValues(SquidlrHeaderNames.Platform, out var headerValues))
                    throw new ApiClientException("Platform header is missing.");

                var platform = Enum.Parse<SocialMediaPlatform>(headerValues.Single());

                return platform switch
                {
                    SocialMediaPlatform.Facebook => (await response.Content.ReadFromJsonAsync<FacebookContent>(cancellationToken: cancellationToken))!,
                    SocialMediaPlatform.Instagram => (await response.Content.ReadFromJsonAsync<InstagramContent>(cancellationToken: cancellationToken))!,
                    SocialMediaPlatform.LinkedIn => (await response.Content.ReadFromJsonAsync<LinkedInContent>(cancellationToken: cancellationToken))!,
                    SocialMediaPlatform.Tiktok => (await response.Content.ReadFromJsonAsync<TiktokContent>(cancellationToken: cancellationToken))!,
                    SocialMediaPlatform.Twitter => (await response.Content.ReadFromJsonAsync<TwitterContent>(cancellationToken: cancellationToken))!,
                    _ => throw new ArgumentException("Unsupported platform: " + platform)
                };
            }

            if (response.Content.Headers.ContentType?.MediaType?.Equals("application/problem+json", StringComparison.OrdinalIgnoreCase) == true)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
                var result = RequestContentResult.Error;
                if (problem!.Extensions.TryGetValue("result", out var resultObject) && resultObject is not null)
                {
                    result = Enum.Parse<RequestContentResult>(resultObject.ToString()!, ignoreCase: true);
                }
                _logger.LogWarning("Failed to get content. Detail: '{ProblemDetail}' Result: '{ProblemResult}'", problem.Detail, result);

                return new(result);
            }

            _logger.LogWarning("Failed to get content. StatusCode: '{ResponseStatusCode}'", response.StatusCode);
            throw new ApiClientException(response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e) when (e is not ApiClientException)
        {
            _logger.LogWarning(e, "Failed to get content due to exception.");
            throw new ApiClientException(e);
        }
    }
}
