using System.IO.Compression;

namespace Squidlr.Hosting.Compression;

internal static class ZstdUtils
{
    internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) => compressionLevel switch
    {
        CompressionLevel.NoCompression => 0,
        CompressionLevel.Fastest => 1,
        CompressionLevel.Optimal => 5,
        CompressionLevel.SmallestSize => 19,
        _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel))
    };
}
