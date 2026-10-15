using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public sealed record GlowPreset(string Name, string Hex);

/// <summary>Pure mapping for the mirror glow border: presets, colour parsing and intensity to edge geometry.</summary>
public static class GlowStyle
{
    public const string DefaultHex = "#FF7F00";
    /// <summary>Opacity of the innermost layer relative to the outermost one.</summary>
    const double InnerFraction = 0.25;

    public static readonly IReadOnlyList<GlowPreset> Presets = new GlowPreset[]
    {
        new("Orange", DefaultHex), new("Red", "#E53935"), new("Blue", "#1E88E5"), new("Green", "#43A047"),
        new("Purple", "#8E24AA"), new("White", "#FFFFFF"), new("Yellow", "#FDD835"),
    };

    /// <summary>Accepts #RRGGBB or #AARRGGBB (the leading # is optional, case is ignored; alpha is dropped).</summary>
    public static bool TryParse(string? hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        var s = hex?.Trim();
        if (string.IsNullOrEmpty(s)) return false;
        if (s[0] == '#') s = s[1..];
        if (s.Length is not (6 or 8) || !s.All(Uri.IsHexDigit)) return false;
        var o = s.Length - 6;
        r = Convert.ToByte(s.Substring(o, 2), 16);
        g = Convert.ToByte(s.Substring(o + 2, 2), 16);
        b = Convert.ToByte(s.Substring(o + 4, 2), 16);
        return true;
    }

    /// <summary>Canonical upper-case #RRGGBB, or the default orange for null/invalid input.</summary>
    public static string Normalize(string? hex) =>
        TryParse(hex, out var r, out var g, out var b) ? $"#{r:X2}{g:X2}{b:X2}" : DefaultHex;

    public static GlowIntensity Normalize(GlowIntensity i) => Enum.IsDefined(i) ? i : GlowIntensity.Medium;

    /// <summary>Width in physical px of the edge the glow is drawn in (the thumbnail is inset by this much).</summary>
    public static int Thickness(GlowIntensity i) => Normalize(i) switch
    {
        GlowIntensity.Low => 2,
        GlowIntensity.High => 5,
        _ => 3,
    };

    /// <summary>Opacity of the outermost layer.</summary>
    public static double PeakOpacity(GlowIntensity i) => Normalize(i) switch
    {
        GlowIntensity.Low => 0.85,
        GlowIntensity.High => 1.0,
        _ => 0.95,
    };

    /// <summary>Opacity per 1px layer, outermost first, fading towards the thumbnail.</summary>
    public static double[] LayerOpacities(GlowIntensity i)
    {
        var n = Thickness(i);
        var peak = PeakOpacity(i);
        var layers = new double[n];
        for (var k = 0; k < n; k++)
            layers[k] = n == 1 ? peak : peak * (1 - (1 - InnerFraction) * k / (n - 1));
        return layers;
    }

    /// <summary>Inset of the thumbnail inside the window: the glow edge when the glow is drawn (enabled and the mirror not locked), otherwise the plain 2px frame.</summary>
    public static int Inset(GlowSettings glow, bool isLocked) => glow.Enabled && !isLocked ? Thickness(glow.Intensity) : 2;

    /// <summary>A copy with colour and intensity repaired.</summary>
    public static GlowSettings Sanitize(GlowSettings? g) =>
        g is null ? new() : g with { Color = Normalize(g.Color), Intensity = Normalize(g.Intensity) };
}
