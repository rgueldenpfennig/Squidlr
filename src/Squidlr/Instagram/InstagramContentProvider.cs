using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNext;
using Squidlr.Abstractions;
using Squidlr.Instagram.Utilities;

namespace Squidlr.Instagram;

public sealed class InstagramContentProvider : IContentProvider
{
    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Instagram;

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
        //client.DefaultRequestHeaders.Add("X-IG-App-ID", "936619743392459

        client.BaseAddress = new Uri("https://i.instagram.com/api/v1");

        var id = UrlUtilities.GetInstagramIdFromUrl(url);
        var response = await client.GetAsync(
            "/web/get_ruling_for_content/?content_type=MEDIA&target_id={_id_to_pk(video_id)}",
            cancellationToken);

        return new Result<Content, RequestContentResult>(RequestContentResult.Error);
    }
}
