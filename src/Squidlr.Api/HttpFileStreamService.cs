using System.Buffers;
using System.Net;
using System.Net.Http.Headers;

namespace Squidlr.Api;

public sealed class HttpFileStreamService
{
    private const int _defaultBufferSize = 65_536;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpFileStreamService> _logger;

    public HttpFileStreamService(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpFileStreamService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask CopyFileStreamAsync(HttpContext httpContext, string httpClientName, string fileName, Uri fileUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpClientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        _logger.LogInformation("Downloading file from: {FileUri}", fileUri);

        var httpClient = _httpClientFactory.CreateClient(httpClientName);
        httpContext.Response.Headers.ContentDisposition = $"attachment; fileName=Squidlr-{fileName}.mp4";

        try
        {
            await CopyFileStreamAsync(fileUri, httpContext, httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                return;

            _logger.LogWarning(ex, "An exception occurred while downloading file: {FileUri}", fileUri);
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadGateway;
        }
    }

    private static async ValueTask CopyFileStreamAsync(Uri fileUri, HttpContext httpContext, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, fileUri);

        if (httpContext.Request.Headers.TryGetValue("Range", out var rangeHeader))
        {
            request.Headers.Range = RangeHeaderValue.Parse(rangeHeader!);
        }

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        httpContext.Response.StatusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength is not null)
        {
            httpContext.Response.ContentLength = response.Content.Headers.ContentLength;
            httpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString();
            if (response.Headers.AcceptRanges?.Count > 0)
            {
                httpContext.Response.Headers.Append("Accept-Ranges", response.Headers.AcceptRanges.ToString());
            }

            using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await CopyStream(httpContext.Response.Body, input, cancellationToken);
        }
    }

    private static async ValueTask CopyStream(Stream output, Stream input, CancellationToken cancellationToken)
    {
        // stream copying taken from https://github.com/microsoft/reverse-proxy/blob/main/src/ReverseProxy/Forwarder/StreamCopier.cs
        var buffer = ArrayPool<byte>.Shared.Rent(_defaultBufferSize);
        int read;
        long contentLength = 0;
        try
        {
            while (true)
            {
                read = 0;

                // Issue a zero-byte read to the input stream to defer buffer allocation until data is available.
                // Note that if the underlying stream does not supporting blocking on zero byte reads, then this will
                // complete immediately and won't save any memory, but will still function correctly.
                var zeroByteReadTask = input.ReadAsync(Memory<byte>.Empty, cancellationToken);
                if (zeroByteReadTask.IsCompletedSuccessfully)
                {
                    // Consume the ValueTask's result in case it is backed by an IValueTaskSource
                    _ = zeroByteReadTask.Result;
                }
                else
                {
                    // Take care not to return the same buffer to the pool twice in case zeroByteReadTask throws
                    var bufferToReturn = buffer;
                    buffer = null;
                    ArrayPool<byte>.Shared.Return(bufferToReturn);

                    await zeroByteReadTask;

                    buffer = ArrayPool<byte>.Shared.Rent(_defaultBufferSize);
                }

                read = await input.ReadAsync(buffer.AsMemory(), cancellationToken);
                contentLength += read;

                // End of the source stream.
                if (read == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        finally
        {
            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
