using TibiaMirror.Models;

namespace TibiaMirror;
public sealed class AppHost : IDisposable
{
    public AppHost(AppPaths paths) { Paths = paths; }
    public AppPaths Paths { get; }
    public AppSettings Settings { get; set; } = new();
    public Profile CurrentProfile { get; set; } = new();
    public TibiaMirror.Abstractions.ISoundPlayer Sounds { get; } = new TibiaMirror.Audio.SoundPlayer();
    public event Action? ToggleMirrorsRequested;   // raised by tray; MirrorService subscribes (Task 7)
    public void RequestToggleMirrors() => ToggleMirrorsRequested?.Invoke();
    public event Action<Profile>? ProfileChanged;
    public void SetProfile(Profile p) { CurrentProfile = p; ProfileChanged?.Invoke(p); }
    public void ProfileEdited() { }      // Task 10 adds debounced autosave
    public void ApplySettings() { }      // Tasks 7/8/10 add hotkey registration
    public Func<bool> IsTibiaFocused { get; set; } = () => false;   // Task 5 assigns
    public TibiaMirror.Hotkeys.KeyboardHook? Keyboard { get; private set; }
    public TibiaMirror.Hotkeys.HotkeyService? Hotkeys { get; private set; }
    public bool HotkeysUnavailable { get; private set; }
    public void Start()   // tasks add service startup here
    {
        try { Keyboard = new TibiaMirror.Hotkeys.KeyboardHook(); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error("Keyboard hook failed", e); HotkeysUnavailable = true; }
        if (Keyboard != null)
        {
            Hotkeys = new TibiaMirror.Hotkeys.HotkeyService(Keyboard, () => IsTibiaFocused(), a => System.Windows.Application.Current.Dispatcher.BeginInvoke(a));
            TibiaMirror.Controls.HotkeyBox.Service = Hotkeys;
            TibiaMirror.Controls.HotkeyBox.Source = Keyboard;
        }
    }
    public void Dispose() // tasks add disposal here
    {
        Hotkeys?.Dispose();
        Keyboard?.Dispose();
        (Sounds as IDisposable)?.Dispose();
    }
}
