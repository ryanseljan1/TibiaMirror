using TibiaMirror.Detection;
using TibiaMirror.Interop;
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
    public void ApplySettings() { ApplyMirrorHotkey(); }      // Tasks 7/8/10 add hotkey registration
    public TibiaMirror.Mirrors.MirrorService? Mirrors { get; private set; }
    public TibiaMirror.Mirrors.MirrorsViewModel MirrorsVm { get; } = new();
    IDisposable? _mirrorHotkey;
    public void ApplyMirrorHotkey()
    {
        _mirrorHotkey?.Dispose();
        _mirrorHotkey = null;
        if (Hotkeys is not null && Settings.ShowHideAllHotkey is { } hk)
            _mirrorHotkey = Hotkeys.Register(this, hk, TibiaMirror.Hotkeys.HotkeyScope.Global, RequestToggleMirrors);
    }
    public Func<bool> IsTibiaFocused { get; set; } = () => false;
    public TibiaMirror.Hotkeys.KeyboardHook? Keyboard { get; private set; }
    public TibiaMirror.Hotkeys.HotkeyService? Hotkeys { get; private set; }
    public bool HotkeysUnavailable { get; private set; }
    public TibiaLocator? Locator { get; private set; }
    public event Action<TibiaState>? TibiaStateChanged;
    System.Windows.Threading.DispatcherTimer? _locatorTimer;
    public void Start()   // tasks add service startup here
    {
        var enumerator = new Win32WindowEnumerator();
        var locator = Locator = new TibiaLocator(enumerator, () => Settings, h => enumerator.GetWindowProcessId(h) == Environment.ProcessId);
        locator.StateChanged += s => TibiaStateChanged?.Invoke(s);
        IsTibiaFocused = () => locator.State.IsFocused;
        _locatorTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _locatorTimer.Tick += (_, _) => locator.Poll();
        _locatorTimer.Start();
        locator.Poll();

        try { Keyboard = new TibiaMirror.Hotkeys.KeyboardHook(); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error("Keyboard hook failed", e); HotkeysUnavailable = true; }
        if (Keyboard != null)
        {
            Hotkeys = new TibiaMirror.Hotkeys.HotkeyService(Keyboard, () => IsTibiaFocused(), a => System.Windows.Application.Current.Dispatcher.BeginInvoke(a));
            TibiaMirror.Controls.HotkeyBox.Service = Hotkeys;
            TibiaMirror.Controls.HotkeyBox.Source = Keyboard;
        }

        Mirrors = new TibiaMirror.Mirrors.MirrorService(locator, () => Settings, ProfileEdited);
        MirrorsVm.Attach(Mirrors);
        Mirrors.Load(CurrentProfile);
        MirrorsVm.Rebuild();
        ToggleMirrorsRequested += () => Mirrors.AllHidden = !Mirrors.AllHidden;
        ProfileChanged += p => { Mirrors.Load(p); MirrorsVm.Rebuild(); };
        ApplySettings();
    }
    public void Dispose() // tasks add disposal here
    {
        _locatorTimer?.Stop();
        _mirrorHotkey?.Dispose();
        Mirrors?.Dispose();
        Hotkeys?.Dispose();
        Keyboard?.Dispose();
        (Sounds as IDisposable)?.Dispose();
    }
}
