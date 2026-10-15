namespace TibiaMirror.Models;

public sealed class RegionModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Region";
    public PixelRect Crop { get; set; }          // Tibia client-area physical px
    public PixelRect Mirror { get; set; }        // screen physical px
    public double Opacity { get; set; } = 1.0;
    public bool IsVisible { get; set; } = true;
    public bool IsLocked { get; set; }
    public CountdownSettings Countdown { get; set; } = new();
    public GlowSettings Glow { get; set; } = new();
}
