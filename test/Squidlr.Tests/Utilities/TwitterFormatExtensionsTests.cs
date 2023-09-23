using System.Globalization;
using Squidlr.Utilities;

namespace Squidlr.Tests.Utilities;

public class TwitterFormatExtensionsTests
{
    [Theory]
    [InlineData("Sat Apr 29 05:11:10 +0000 2023", "04/29/2023 05:11:10 +00:00")]
    [InlineData("Tue Dec 01 22:35:28 +0000 2015", "12/01/2015 22:35:28 +00:00")]
    public void ParseToDateTimeOffset(string value, string expectedResult)
    {
        // Act
        var result = TwitterFormatExtensions.ParseToDateTimeOffset(value);

        // Assert
        Assert.Equal(expectedResult, result.ToString(CultureInfo.InvariantCulture));
    }
}