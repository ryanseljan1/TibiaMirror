using TibiaMirror.Mirrors; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Mirrors;

public class GlowStyleTests
{
    [Theory]
    [InlineData("#FF7F00", "#FF7F00")]
    [InlineData("ff7f00", "#FF7F00")]
    [InlineData("  #1e88e5 ", "#1E88E5")]
    [InlineData("#80112233", "#112233")]   // alpha dropped
    public void Normalize_canonicalises_valid_hex(string input, string expected) => Assert.Equal(expected, GlowStyle.Normalize(input));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("   ")] [InlineData("#FFF")] [InlineData("#GG0000")]
    [InlineData("#FF7F0")] [InlineData("red")] [InlineData("#FF7F00AA11")]
    public void Normalize_falls_back_to_orange_for_invalid_hex(string? input) => Assert.Equal("#FF7F00", GlowStyle.Normalize(input));

    [Fact] public void TryParse_returns_channels()
    {
        Assert.True(GlowStyle.TryParse("#0A141E", out var r, out var g, out var b));
        Assert.Equal((10, 20, 30), (r, g, b));
        Assert.False(GlowStyle.TryParse("nope", out _, out _, out _));
    }

    [Fact] public void Presets_are_the_seven_named_colours_all_valid()
    {
        Assert.Equal(new[] { "Orange", "Red", "Blue", "Green", "Purple", "White", "Yellow" }, GlowStyle.Presets.Select(p => p.Name));
        Assert.Equal("#FF7F00", GlowStyle.Presets[0].Hex);
        foreach (var p in GlowStyle.Presets) Assert.Equal(p.Hex, GlowStyle.Normalize(p.Hex));
    }

    [Theory]
    [InlineData(GlowIntensity.Low, 2)] [InlineData(GlowIntensity.Medium, 3)] [InlineData(GlowIntensity.High, 5)]
    public void Thickness_grows_with_intensity(GlowIntensity i, int expected) => Assert.Equal(expected, GlowStyle.Thickness(i));

    [Fact] public void Undefined_intensity_behaves_as_medium()
    {
        Assert.Equal(GlowIntensity.Medium, GlowStyle.Normalize((GlowIntensity)99));
        Assert.Equal(3, GlowStyle.Thickness((GlowIntensity)99));
    }

    [Theory]
    [InlineData(GlowIntensity.Low)] [InlineData(GlowIntensity.Medium)] [InlineData(GlowIntensity.High)]
    public void Layers_fade_inwards_and_match_thickness(GlowIntensity i)
    {
        var layers = GlowStyle.LayerOpacities(i);
        Assert.Equal(GlowStyle.Thickness(i), layers.Length);
        Assert.Equal(GlowStyle.PeakOpacity(i), layers[0], 6);
        for (var k = 1; k < layers.Length; k++) Assert.True(layers[k] < layers[k - 1]);
        Assert.All(layers, o => Assert.InRange(o, 0.0, 1.0));
    }

    [Fact] public void Stronger_intensity_is_never_dimmer() =>
        Assert.True(GlowStyle.PeakOpacity(GlowIntensity.Low) <= GlowStyle.PeakOpacity(GlowIntensity.Medium)
                    && GlowStyle.PeakOpacity(GlowIntensity.Medium) <= GlowStyle.PeakOpacity(GlowIntensity.High));

    [Fact] public void Inset_is_glow_edge_when_enabled_else_plain_frame()
    {
        Assert.Equal(5, GlowStyle.Inset(new GlowSettings { Intensity = GlowIntensity.High }));
        Assert.Equal(2, GlowStyle.Inset(new GlowSettings { Enabled = false, Intensity = GlowIntensity.High }));
    }

    [Fact] public void Sanitize_repairs_colour_and_intensity_and_handles_null()
    {
        var g = GlowStyle.Sanitize(new GlowSettings { Color = "junk", Intensity = (GlowIntensity)42, Enabled = false });
        Assert.Equal(new GlowSettings { Color = "#FF7F00", Intensity = GlowIntensity.Medium, Enabled = false }, g);
        Assert.Equal(new GlowSettings(), GlowStyle.Sanitize(null));
    }
}
