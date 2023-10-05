using DotNext;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
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

    public async ValueTask<Result<TweetContent, GetTweetVideoResult>> GetTweetContentAsync(TweetIdentifier tweetIdentifier, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("TweetId", tweetIdentifier.Id))
        {
            _logger.LogInformation("Requesting Tweet content for '{TweetUrl}'", tweetIdentifier.Url);

            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientName);
                var response = await client.GetAsync($"/content?url={tweetIdentifier.Url}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _telemetryHandler.TrackEvent("ContentRequested", new Dictionary<string, string> { { "Url", tweetIdentifier.Url } });
                    var content = await response.Content.ReadFromJsonAsync<TweetContent>(cancellationToken: cancellationToken);
                    return content!;
                }

                if (response.Content.Headers.ContentType?.MediaType?.Equals("application/problem+json", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
                    {
                        var result = GetTweetVideoResult.Error;
                        if (problem!.Extensions.TryGetValue("result", out var resultObject) && resultObject is not null)
                        {
                            result = Enum.Parse<GetTweetVideoResult>(resultObject.ToString()!, ignoreCase: true);
                        }
                        _logger.LogWarning("Failed to get Tweet content. Detail: '{ProblemDetail}' Result: '{ProblemResult}'", problem.Detail, result);

                        return new(result);
                    }
                }

                _logger.LogWarning("Failed to get Tweet content. StatusCode: '{ResponseStatusCode}'", response.StatusCode);
                throw new ApiClientException(response.StatusCode);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to get Tweet content due to exception.");
                throw new ApiClientException(e);
            }
        }
    }
}
