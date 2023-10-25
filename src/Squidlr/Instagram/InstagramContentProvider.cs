using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using DotNext;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Instagram.Utilities;

namespace Squidlr.Instagram;

public sealed class InstagramContentProvider : IContentProvider
{
    private readonly ILogger<InstagramContentProvider> _logger;

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Instagram;

    public InstagramContentProvider(ILogger<InstagramContentProvider> logger)
    {
        _logger = logger;
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("accept", "*/*");
        client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");
        client.DefaultRequestHeaders.Add("sec-ch-ua", "Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \"Not:A-Brand\";v=\"99\"");
        client.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
        client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        client.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("X-Ig-App-Id", "936619743392459");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        client.DefaultRequestHeaders.Add("Referer", url);

        client.BaseAddress = new Uri("https://www.instagram.com");

        var identifier = UrlUtilities.GetInstagramIdentifier(url);

        using var response = await client.GetAsync(
            $"/graphql/query/?query_hash=9f8827793ef34641b2fb195d4d41151c&variables={UrlEncoder.Default.Encode($$"""{"shortcode":"{{identifier.Id}}"}""")}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {InstagramHttpStatusCode} HTTP status code when trying to request Instagram post.",
                response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new(RequestContentResult.NotFound);

            return new(RequestContentResult.GatewayError);
        }

        var streamCopy = new MemoryStream(
            response.Content.Headers.ContentLength != null ?
            (int)response.Content.Headers.ContentLength : 32_768);

        await response.Content.CopyToAsync(streamCopy, cancellationToken);
        streamCopy.Position = 0;

        using var document = JsonDocument.Parse(streamCopy);
        var root = document.RootElement;

        return new Result<Content, RequestContentResult>(RequestContentResult.Error);
    }
}
