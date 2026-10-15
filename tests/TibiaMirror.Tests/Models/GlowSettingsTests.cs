using System.Text.Json; using TibiaMirror.Models; using TibiaMirror.Persistence; using TibiaMirror.Profiles;
namespace TibiaMirror.Tests.Models;

public class GlowSettingsTests
{
    [Fact] public void Region_json_without_glow_loads_with_visible_orange_border_defaults()
    {
        var r = JsonSerializer.Deserialize<RegionModel>("""{ "Name": "Old", "Opacity": 0.8, "IsLocked": true }""", JsonStore.Options)!;
        Assert.NotNull(r.Glow);
        Assert.True(r.Glow.Enabled);
        Assert.Equal("#FF7F00", r.Glow.Color);
        Assert.Equal(GlowIntensity.Medium, r.Glow.Intensity);
    }

    [Fact] public void Glow_round_trips_with_enum_names()
    {
        var r = new RegionModel { Glow = new GlowSettings { Enabled = false, Color = "#1E88E5", Intensity = GlowIntensity.High } };
        var json = JsonSerializer.Serialize(r, JsonStore.Options);
        Assert.Contains("\"High\"", json);
        Assert.Equal(r.Glow, JsonSerializer.Deserialize<RegionModel>(json, JsonStore.Options)!.Glow);
    }

    [Fact] public void Profile_normalizer_repairs_missing_or_bad_glow()
    {
        var p = new Profile { Regions = { new RegionModel { Glow = null! }, new RegionModel { Glow = new GlowSettings { Color = "zzz", Intensity = (GlowIntensity)7 } } } };
        ProfileNormalizer.Normalize(p);
        Assert.Equal(new GlowSettings(), p.Regions[0].Glow);
        Assert.Equal(new GlowSettings(), p.Regions[1].Glow);
    }

    [Fact] public void Profile_json_with_null_glow_is_normalized()
    {
        var p = JsonSerializer.Deserialize<Profile>("""{ "Name": "P", "Regions": [ { "Name": "A", "Glow": null } ] }""", JsonStore.Options)!;
        ProfileNormalizer.Normalize(p);
        Assert.Equal(new GlowSettings(), p.Regions[0].Glow);
    }
}
