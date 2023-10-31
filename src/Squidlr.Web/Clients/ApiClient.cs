using DotNext;
using Microsoft.AspNetCore.Mvc;
using Squidlr.Instagram;
using Squidlr.Twitter;
using Squidlr.Web.Telemetry;

namespace Squidlr.Web.Clients;

public sealed class ApiClient
{
    public const string HttpClientName = nameof(ApiClient);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TelemetryHandler _telemetryHandler;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(IHttpClientFactory httpClientFactory, TelemetryHandler telemetryHandler, ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _telemetryHandler = telemetryHandler ?? throw new ArgumentNullException(nameof(telemetryHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        _logger.LogInformation("Requesting content for '{ContentUrl}'", url);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var response = await client.GetAsync($"/content?url={url}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _telemetryHandler.TrackEvent("ContentRequested", new Dictionary<string, string> { { "Url", url } });
                if (!response.Headers.TryGetValues("X-Squidlr-Platform", out var headerValues))
                    throw new ApiClientException("Platform header is missing.");

                var platform = Enum.Parse<SocialMediaPlatform>(headerValues.Single());
                return platform switch
                {
                    SocialMediaPlatform.Instagram => (await response.Content.ReadFromJsonAsync<InstagramContent>(cancellationToken: cancellationToken))!,
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
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to get content due to exception.");
            throw new ApiClientException(e);
        }
    }
}
