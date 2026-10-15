using TibiaMirror.Audio;
namespace TibiaMirror.Tests.Audio;

public class SoundFormatsTests
{
    [Theory]
    [InlineData("a.mp3")] [InlineData("a.WAV")] [InlineData("a.m4a")] [InlineData("C:\\x\\a.aac")] [InlineData("a.FLAC")]
    public void Supported(string p) => Assert.True(SoundFormats.IsSupported(p));

    [Theory]
    [InlineData("a.ogg")] [InlineData("a.txt")] [InlineData("a")] [InlineData("")] [InlineData(null)]
    public void Unsupported(string? p) => Assert.False(SoundFormats.IsSupported(p));

    [Fact] public void Filter_lists_each_extension_and_omits_ogg()
    {
        var f = SoundFormats.DialogFilter;
        foreach (var e in new[] { "*.mp3", "*.wav", "*.m4a", "*.aac", "*.flac" }) Assert.Contains(e, f);
        Assert.DoesNotContain("ogg", f);
    }

    [Fact] public void Media_foundation_used_for_non_native_formats()
    {
        Assert.True(SoundFormats.UsesMediaFoundation("a.m4a")); Assert.False(SoundFormats.UsesMediaFoundation("a.mp3"));
    }
}
