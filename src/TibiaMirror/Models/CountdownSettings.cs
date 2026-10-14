namespace TibiaMirror.Models;

public enum BarSide { Top, Bottom, Left, Right }
public enum BarShape { FullEdge, NarrowStrip }

public sealed class CountdownSettings
{
    public bool Enabled { get; set; }
    public Hotkey? Hotkey { get; set; }
    public double DurationSeconds { get; set; } = 10;
    public BarSide Side { get; set; } = BarSide.Bottom;
    public BarShape Shape { get; set; } = BarShape.FullEdge;
    public string Color { get; set; } = "#FFFF7F00";
    public int Thickness { get; set; } = 6;
    public bool FlashOnDone { get; set; } = true;
    public string? Sound { get; set; }
}
