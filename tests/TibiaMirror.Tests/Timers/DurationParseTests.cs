using System.Globalization;
using TibiaMirror.Timers;
namespace TibiaMirror.Tests.Timers;

public class DurationParseTests
{
    [Theory]
    [InlineData("1.5", 1.5)] [InlineData("1,5", 1.5)] [InlineData(" 10 ", 10)] [InlineData("0.1", 0.1)] [InlineData("3600", 3600)]
    public void Accepts_valid(string text, double expected)
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            foreach (var c in new[] { "en-US", "de-DE" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(c);
                Assert.True(TimerEditDialog.TryParseDuration(text, out var v));
                Assert.Equal(expected, v, 3);
            }
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory] [InlineData("")] [InlineData("abc")] [InlineData("0.05")] [InlineData("3601")] [InlineData("-1")] [InlineData("NaN")]
    public void Rejects_invalid(string text) => Assert.False(TimerEditDialog.TryParseDuration(text, out _));
}
