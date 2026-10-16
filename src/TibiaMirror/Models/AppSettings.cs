namespace TibiaMirror.Models;

public sealed class AppSettings : TibiaMirror.Persistence.IVersioned
{
    public int SchemaVersion { get; set; } = 1;
    public Hotkey? ShowHideAllHotkey { get; set; }
    public Hotkey? NextProfileHotkey { get; set; }
    public Hotkey? LockAllHotkey { get; set; }
    public bool OnlyShowWhenTibiaFocused { get; set; }
    public bool ClickThroughWhileTibiaFocused { get; set; } = true;
    /// <summary>Legacy (pre-0.2.0) setting, only read to migrate to <see cref="ThemeMode"/>.</summary>
    public bool? DarkMode { get; set; }
    public ThemeMode? ThemeMode { get; set; }
    public string TibiaProcessName { get; set; } = "client";
    public string TibiaTitlePrefix { get; set; } = "Tibia";
    public bool TrayHintShown { get; set; }
    public PixelRect? MainWindowBounds { get; set; }
}
