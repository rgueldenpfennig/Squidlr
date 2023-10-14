using System.Net;

namespace Squidlr.Web.Clients;

public sealed class StreamVideoResult
{
    public HttpStatusCode HttpStatusCode { get; init; }

    public RequestVideoResult Result { get; init; }

    public Stream? Stream { get; init; }

    public HttpResponseMessage? Response { get; init; }
}
