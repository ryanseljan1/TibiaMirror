using TibiaMirror.Detection;
using TibiaMirror.Interop;
using TibiaMirror.Models;

namespace TibiaMirror;
public sealed class AppHost : IDisposable
{
    public AppHost(AppPaths paths)
    {
        Paths = paths; MirrorsVm = new TibiaMirror.Mirrors.MirrorsViewModel(Snackbar);_autosave = new TibiaMirror.Persistence.Debouncer(TimeSpan.FromSeconds(1), SaveAll);
        Updates = new(new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(10) }, () => Settings, AppInfo.Version, Environment.ProcessPath,
            () => System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => ExitRequested?.Invoke()));
    }
    /// <summary>Update check/install (DIST-01). The UI lives in Updates/ and App.xaml.cs; Settings has "Check now".</summary>
    public TibiaMirror.Updates.UpdateService Updates { get; }
    /// <summary>Raised (on the UI thread) when the app must exit, e.g. to let the updater install.</summary>
    public event Action? ExitRequested;
    public AppPaths Paths { get; }
    readonly AppSettings _defaultSettings = new();   // used until SettingsService.Load() has run
    public AppSettings Settings => SettingsService?.Current ?? _defaultSettings;
    public Profile CurrentProfile { get; set; } = new();
    public TibiaMirror.Abstractions.ISoundPlayer Sounds { get; } = new TibiaMirror.Audio.SoundPlayer();
    /// <summary>Hosts the "deleted - Undo" snackbar shown at the bottom of the main window.</summary>
    public TibiaMirror.Controls.SnackbarService Snackbar { get; } = new(new TibiaMirror.Abstractions.SystemClock());
    public event Action? ToggleMirrorsRequested;   // raised by tray; MirrorService subscribes (Task 7)
    public void RequestToggleMirrors() => ToggleMirrorsRequested?.Invoke();
    public event Action<Profile>? ProfileChanged;
    public void SetProfile(Profile p) { ClampToMonitors(p); CurrentProfile = p; ProfileChanged?.Invoke(p); }
    public void ProfileEdited() => _autosave.Trigger();
    public void ApplySettings() { ApplyMirrorHotkey(); ApplyLockAllHotkey(); ApplyProfileHotkey(); Mirrors?.Refresh(); TibiaMirror.Settings.ThemeManager.Apply(Settings.ThemeMode ?? TibiaMirror.Models.ThemeMode.Dark); }

    // --- Task 10: autosave, restore, hotkey retry ---
    public TibiaMirror.Settings.SettingsService? SettingsService { get; private set; }
    readonly TibiaMirror.Persistence.Debouncer _autosave;
    public event Action? HotkeysReady;              // raised after Retry recreated the hook + HotkeyService
    public event Action? HotkeysUnavailableChanged;

    void ClampToMonitors(Profile p)
    {
        try { TibiaMirror.Settings.RestoreClamp.ClampProfile(p, Monitors.WorkAreas(), Monitors.PrimaryWorkArea()); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error("Monitor clamp failed", e); }
    }

    /// <summary>Clamps a saved window rectangle (physical px) onto the current monitors.</summary>
    public PixelRect ClampToMonitors(PixelRect r)
    {
        try { return TibiaMirror.Settings.RestoreClamp.Clamp(r, Monitors.WorkAreas(), Monitors.PrimaryWorkArea()); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error("Monitor clamp failed", e); return r; }
    }

    void SaveAll()
    {
        var d = System.Windows.Application.Current?.Dispatcher;
        if (d is null || d.CheckAccess() || d.HasShutdownStarted) SaveNow();
        else d.Invoke(SaveNow);   // UI-thread snapshot of profile + settings
    }

    /// <summary>Saves profile and settings; throws if either failed so the Debouncer logs it and re-arms (a later Trigger/Flush retries).</summary>
    void SaveNow() => TibiaMirror.Settings.AutosaveRunner.SaveBoth(() => Profiles?.SaveCurrent(), () => SettingsService?.Save());

    /// <summary>Recreates the keyboard hook and hotkey service and re-registers every consumer. Returns success.</summary>
    public bool RetryHotkeys()
    {
        try
        {
            var kb = new TibiaMirror.Hotkeys.KeyboardHook();
            Hotkeys?.Dispose();
            Keyboard?.Dispose();
            Keyboard = kb;
            Hotkeys = new TibiaMirror.Hotkeys.HotkeyService(kb, () => IsTibiaFocused(), a => System.Windows.Application.Current.Dispatcher.BeginInvoke(a));
            TibiaMirror.Controls.HotkeyBox.Service = Hotkeys;
            TibiaMirror.Controls.HotkeyBox.Source = Keyboard;
            HotkeysUnavailable = false;
            ApplySettings();
            HotkeysReady?.Invoke();
            HotkeysUnavailableChanged?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            TibiaMirror.Logging.Log.Error("Keyboard hook retry failed", e);
            return false;
        }
    }

    // --- Task 8: profiles ---
    public TibiaMirror.Profiles.ProfileService? Profiles { get; private set; }
    IDisposable? _profileHotkeyReg;
    public TibiaMirror.Hotkeys.GlobalHotkeyOwner ShowHideOwner { get; } = new(TibiaMirror.Localization.Loc.Get("Common_Hotkey_ShowHide"));
    public TibiaMirror.Hotkeys.GlobalHotkeyOwner NextProfileOwner { get; } = new(TibiaMirror.Localization.Loc.Get("Common_Hotkey_NextProfile"));
    public TibiaMirror.Hotkeys.GlobalHotkeyOwner LockAllOwner { get; } = new(TibiaMirror.Localization.Loc.Get("Common_Hotkey_LockAll"));
    readonly List<string> _pendingNotices = new();
    bool _noticesReady;

    public void ApplyProfileHotkey()
    {
        _profileHotkeyReg?.Dispose();
        _profileHotkeyReg = null;
        if (Settings.NextProfileHotkey is { } hk && Hotkeys != null)
            _profileHotkeyReg = Hotkeys.Register(NextProfileOwner, hk, TibiaMirror.Hotkeys.HotkeyScope.Global, () =>
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
        try { TibiaMirror.Profiles.Toast.ShowToast(TibiaMirror.Localization.Loc.Format("Common_ProfileToast", name)); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error("Toast failed", e); }
    });

    // --- Task 7: mirrors ---
    public TibiaMirror.Mirrors.MirrorService? Mirrors { get; private set; }
    public TibiaMirror.Mirrors.MirrorsViewModel MirrorsVm { get; }
    IDisposable? _mirrorHotkey;
    public void ApplyMirrorHotkey()
    {
        _mirrorHotkey?.Dispose();
        _mirrorHotkey = null;
        if (Hotkeys is not null && Settings.ShowHideAllHotkey is { } hk)
            _mirrorHotkey = Hotkeys.Register(ShowHideOwner, hk, TibiaMirror.Hotkeys.HotkeyScope.Global, RequestToggleMirrors);
    }
    IDisposable? _lockAllHotkey;
    public void ApplyLockAllHotkey()
    {
        _lockAllHotkey?.Dispose();
        _lockAllHotkey = null;
        if (Hotkeys is not null && Settings.LockAllHotkey is { } hk)
            _lockAllHotkey = Hotkeys.Register(LockAllOwner, hk, TibiaMirror.Hotkeys.HotkeyScope.Global, () => Mirrors?.ToggleLockAll());
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
        SettingsService = new TibiaMirror.Settings.SettingsService(Paths);
        SettingsService.Load();
        if (SettingsService.LastLoadStatus == TibiaMirror.Persistence.LoadStatus.RestoredFromBackup)
            QueueNotice(TibiaMirror.Localization.Loc.Get("Common_SettingsRestored"));
        else if (SettingsService.LastLoadStatus == TibiaMirror.Persistence.LoadStatus.Corrupt)
            QueueNotice(TibiaMirror.Localization.Loc.Format("Common_SettingsCorrupt", SettingsService.CorruptPath));
        Profiles = new TibiaMirror.Profiles.ProfileService(Paths);
        Profiles.Notice += QueueNotice;
        try { Profiles.LoadInitial(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { TibiaMirror.Logging.Log.Error("Profile load failed", e); QueueNotice(TibiaMirror.Localization.Loc.Format("Common_ProfilesError", e.Message)); }
        SetProfile(Profiles.Current);
        Profiles.CurrentChanged += p => { SetProfile(p); ShowToast(p.Name); };
        System.Windows.Application.Current.Dispatcher.BeginInvoke(FlushNotices, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

        var enumerator = new Win32WindowEnumerator();
        var locator = Locator = new TibiaLocator(enumerator, () => Settings, h => Mirrors?.IsMirrorWindow(h) == true);
        locator.StateChanged += s => TibiaStateChanged?.Invoke(s);
        IsTibiaFocused = () => FocusRule.IsFocused(NativeMethods.GetForegroundWindow(), locator.State.Window?.Handle, h => Mirrors?.IsMirrorWindow(h) == true);   // live foreground, O(1)
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
        _timersTimer.Tick += (_, _) => { Timers.Tick(); TimersVm.Refresh(); Snackbar.Tick(); };
        _timersTimer.Start();

        Mirrors = new TibiaMirror.Mirrors.MirrorService(locator, () => Settings, ProfileEdited);
        Countdowns = new TibiaMirror.Countdown.CountdownController(this, Mirrors, MirrorsVm);   // Task 11: before Load/Attach so it sees layouts and cards
        Mirrors.Load(CurrentProfile);
        MirrorsVm.Attach(Mirrors);
        TibiaStateChanged += _ => MirrorsVm.OnTibiaStateChanged();   // enables/disables Add Region and Re-crop
        ProfileChanged += _ => Snackbar.Dismiss();                   // an Undo for another profile's item no longer applies
        ToggleMirrorsRequested += () => Mirrors.AllHidden = !Mirrors.AllHidden;
        ProfileChanged += p => { Mirrors.Load(p); MirrorsVm.Rebuild(); };
        ProfileChanged += Countdowns.OnProfileChanged;
        Countdowns.OnProfileChanged(CurrentProfile);
        ApplySettings();
        HotkeysUnavailableChanged?.Invoke();
    }
    public void Dispose() // tasks add disposal here
    {
        _autosave.Flush();
        _autosave.Dispose();
        _locatorTimer?.Stop();
        _profileHotkeyReg?.Dispose();
        _timersTimer?.Stop();
        TimersVm?.Dispose();
        _mirrorHotkey?.Dispose();
        _lockAllHotkey?.Dispose();
        Countdowns?.Dispose();
        Mirrors?.Dispose();
        Hotkeys?.Dispose();
        Keyboard?.Dispose();
        (Sounds as IDisposable)?.Dispose();
    }
}
