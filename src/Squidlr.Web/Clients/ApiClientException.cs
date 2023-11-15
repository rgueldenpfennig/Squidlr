using System.Net;

namespace Squidlr.Web.Clients;

[Serializable]
public class ApiClientException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public ApiClientException(string message) : base(message) { }

    public ApiClientException(Exception innerException) : base("The API could not be accessed due to an exception.", innerException) { }

    public ApiClientException(HttpStatusCode statusCode) : base("The API did not return a success status code.")
    {
        StatusCode = statusCode;
    }
}
