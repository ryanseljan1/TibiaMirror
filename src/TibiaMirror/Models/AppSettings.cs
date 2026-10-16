namespace TibiaMirror.Models;

public sealed class AppSettings : TibiaMirror.Persistence.IVersioned
{
    public int SchemaVersion { get; set; } = 1;
    public Hotkey? ShowHideAllHotkey { get; set; }
    public Hotkey? NextProfileHotkey { get; set; }
    public Hotkey? LockAllHotkey { get; set; }
    public bool OnlyShowWhenTibiaFocused { get; set; }
    public bool ClickThroughWhileTibiaFocused { get; set; } = true;
    public bool DarkMode { get; set; }
    public string TibiaProcessName { get; set; } = "client";
    public string TibiaTitlePrefix { get; set; } = "Tibia";
    public bool TrayHintShown { get; set; }
    public PixelRect? MainWindowBounds { get; set; }
    public bool? CheckForUpdates { get; set; }
    public string? SkippedVersion { get; set; }
}
