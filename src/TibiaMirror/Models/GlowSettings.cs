namespace TibiaMirror.Models;

public enum GlowIntensity { Low, Medium, High }

/// <summary>Per-mirror glow border. Every field has a default so profiles saved before the glow existed load with the old orange border.</summary>
public sealed record GlowSettings
{
    public bool Enabled { get; set; } = true;
    public string Color { get; set; } = "#FF7F00";   // #RRGGBB; anything unparsable falls back to orange when rendered
    public GlowIntensity Intensity { get; set; } = GlowIntensity.Medium;
}
