using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using ZstdSharp;

namespace Squidlr.Hosting.Compression;

/// <summary>
/// ZStandard compression provider.
/// </summary>
public sealed class ZstdCompressionProvider : ICompressionProvider
{
    /// <inheritdoc />
    public string EncodingName { get; } = "zstd";

    /// <inheritdoc />
    public bool SupportsFlush { get; } = true;

    private ZstdCompressionProviderOptions Options { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ZstdCompressionProvider"/> with options.
    /// </summary>
    /// <param name="options">The options for this instance.</param>
    public ZstdCompressionProvider(IOptions<ZstdCompressionProviderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options.Value;
    }

    /// <inheritdoc />
    public Stream CreateStream(Stream outputStream)
    {
        var level = ZstdUtils.GetQualityFromCompressionLevel(Options.Level);
        return new CompressionStream(outputStream, level: level, leaveOpen: true);
    }
}
