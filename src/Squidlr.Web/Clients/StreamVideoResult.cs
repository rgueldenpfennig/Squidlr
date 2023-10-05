using System.Net;
using Squidlr.Twitter;

namespace Squidlr.Web.Clients;

public sealed class StreamVideoResult
{
    public HttpStatusCode HttpStatusCode { get; init; }

    public GetTweetVideoResult Result { get; init; }

    public Stream? Stream { get; init; }

    public HttpResponseMessage? Response { get; init; }
}
