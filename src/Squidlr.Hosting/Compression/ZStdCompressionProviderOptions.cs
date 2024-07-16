using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Squidlr.Hosting.Compression;

/// <summary>
/// Options for the <see cref="ZstdCompressionProvider"/>
/// </summary>
public sealed class ZstdCompressionProviderOptions : IOptions<ZstdCompressionProviderOptions>
{
    /// <summary>
    /// What level of compression to use for the stream. The default is <see cref="CompressionLevel.Fastest"/>.
    /// </summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Fastest;

    /// <inheritdoc />
    ZstdCompressionProviderOptions IOptions<ZstdCompressionProviderOptions>.Value => this;
}
