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
    public void ApplySettings() { ApplyMirrorHotkey(); ApplyProfileHotkey(); Mirrors?.Refresh(); }      // Task 10 extends

    // --- Task 8: profiles ---
    public TibiaMirror.Profiles.ProfileService? Profiles { get; private set; }
    IDisposable? _profileHotkeyReg;
    readonly List<string> _pendingNotices = new();
    bool _noticesReady;

    public void ApplyProfileHotkey()
    {
        _profileHotkeyReg?.Dispose();
        _profileHotkeyReg = null;
        if (Settings.NextProfileHotkey is { } hk && Hotkeys != null)
            _profileHotkeyReg = Hotkeys.Register(this, hk, TibiaMirror.Hotkeys.HotkeyScope.Global, () =>
            {
                try { Profiles?.SwitchNext(); }
                catch (Exception e) { TibiaMirror.Logging.Log.Error("Next-profile hotkey failed", e); }
            });
    }

    /// <summary>Notices raised before the main window is shown are queued, then shown once the UI is idle.</summary>
    void QueueNotice(string message)
    {
        if (_noticesReady) System.Windows.MessageBox.Show(message, AppInfo.Name, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        else _pendingNotices.Add(message);
    }

    void FlushNotices()
    {
        _noticesReady = true;
        var pending = _pendingNotices.ToList();
        _pendingNotices.Clear();
        foreach (var m in pending) QueueNotice(m);
    }

    void ShowToast(string name) => System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
    {
        try { TibiaMirror.Profiles.Toast.ShowToast("Profile: " + name); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error("Toast failed", e); }
    });

    // --- Task 7: mirrors ---
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
    public TibiaMirror.Timers.AudioTimerService? Timers { get; private set; }
    public TibiaMirror.Timers.AudioTimersViewModel? TimersVm { get; private set; }
    System.Windows.Threading.DispatcherTimer? _timersTimer;
    public TibiaMirror.Countdown.CountdownController? Countdowns { get; private set; }   // Task 11
    public void Start()   // tasks add service startup here
    {
        Profiles = new TibiaMirror.Profiles.ProfileService(Paths);
        Profiles.Notice += QueueNotice;
        try { Profiles.LoadInitial(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { TibiaMirror.Logging.Log.Error("Profile load failed", e); QueueNotice("Could not read or write your profiles: " + e.Message); }
        SetProfile(Profiles.Current);
        Profiles.CurrentChanged += p => { SetProfile(p); ShowToast(p.Name); };
        System.Windows.Application.Current.Dispatcher.BeginInvoke(FlushNotices, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

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

        Timers = new TibiaMirror.Timers.AudioTimerService(new TibiaMirror.Abstractions.SystemClock(), Sounds);
        TimersVm = new TibiaMirror.Timers.AudioTimersViewModel(this, Timers);
        _timersTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timersTimer.Tick += (_, _) => { Timers.Tick(); TimersVm.Refresh(); };
        _timersTimer.Start();

        Mirrors = new TibiaMirror.Mirrors.MirrorService(locator, () => Settings, ProfileEdited);
        Countdowns = new TibiaMirror.Countdown.CountdownController(this, Mirrors, MirrorsVm);   // Task 11: before Load/Attach so it sees layouts and cards
        Mirrors.Load(CurrentProfile);
        MirrorsVm.Attach(Mirrors);
        ToggleMirrorsRequested += () => Mirrors.AllHidden = !Mirrors.AllHidden;
        ProfileChanged += p => { Mirrors.Load(p); MirrorsVm.Rebuild(); };
        ProfileChanged += Countdowns.OnProfileChanged;
        Countdowns.OnProfileChanged(CurrentProfile);
        ApplySettings();
    }
    public void Dispose() // tasks add disposal here
    {
        _locatorTimer?.Stop();
        _profileHotkeyReg?.Dispose();
        _timersTimer?.Stop();
        TimersVm?.Dispose();
        _mirrorHotkey?.Dispose();
        Countdowns?.Dispose();
        Mirrors?.Dispose();
        Hotkeys?.Dispose();
        Keyboard?.Dispose();
        (Sounds as IDisposable)?.Dispose();
    }
}
