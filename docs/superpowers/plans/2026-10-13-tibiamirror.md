# TibiaMirror Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build TibiaMirror, a Windows app that mirrors parts of the Tibia client on screen with audio timers, per-mirror countdown bars and profiles, shipped through GitHub Releases.

**Architecture:** C# .NET 8 WPF single executable. Mirrors are DWM thumbnails of the Tibia window hosted in borderless topmost windows. A single low-level keyboard hook *observes* keys for hotkeys. Services are plain classes with small interfaces over Win32 so logic is unit-tested; `AppHost` is the composition root.

**Tech Stack:** .NET 8 (`net8.0-windows`, WPF, x64), CommunityToolkit.Mvvm 8.x, NAudio 2.2.x, xUnit, Inno Setup 6, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-10-13-tibiamirror-design.md` — read it before your task; this plan argues from it.

## Global Constraints

- Target `net8.0-windows`, `<UseWPF>true</UseWPF>`, `RuntimeIdentifier win-x64`, nullable enabled, `LangVersion latest`, warnings as errors for nullable (`<WarningsAsErrors>nullable</WarningsAsErrors>`).
- App name, data-folder name and version are defined only in `src/TibiaMirror/AppInfo.cs` and `Directory.Build.props`.
- Data folder: `%APPDATA%\TibiaMirror\` → `settings.json`, `profiles\<name>.json`, `last_profile`, `logs\`.
- Game safety: never read/write Tibia memory, inject, send input, or swallow keys. The keyboard hook always calls `CallNextHookEx` and returns its result.
- No TibiaVision code, text, assets, sounds or branding. Built-in sounds are synthesized by our own code.
- Tibia detection defaults: process name `client` (case-insensitive), title prefix `Tibia`.
- Crop stored in Tibia **client-area physical pixels**; mirror bounds in **screen physical pixels**.
- Persistence: 1 s debounce + save on exit; atomic write (`*.tmp` → `File.Replace` with `*.bak`); `SchemaVersion` = 1 in every file.
- Countdown `DurationSeconds` 0.5–3600; audio timer `DurationSeconds` 0.1–3600; `Thickness` 2–40; `Opacity` 0.1–1.0; profile names 1–40 chars, unique case-insensitively, no `Path.GetInvalidFileNameChars()`; region min 8×8 px.
- Effective timer volume = `MasterVolume/100 × timer.Volume/100`.
- License MIT. Every `.cs` file uses file-scoped namespaces under `TibiaMirror.*`.
- Test command for every task: `dotnet test tests/TibiaMirror.Tests -c Debug` (must be green before commit). Build command: `dotnet build TibiaMirror.sln -c Debug` (0 warnings-as-errors).

## Review Focus

1. **Tibia window moved/resized after a crop was drawn** → mirrors keep showing the same game area. Pinned in Task 2 (`CropMath` tests with window ≠ client origin and moved windows).
2. **Saved mirror on a monitor that is no longer connected** → mirror appears on the primary monitor, fully visible. Pinned in Task 2 (`MonitorClamp` tests).
3. **App killed mid-save / hand-edited broken JSON** → last good state restored from `.bak`, else `*.corrupt` + empty data, never a crash. Pinned in Task 3.
4. **Pressing a timer hotkey while typing in Discord/browser, or Ctrl+F1 when the timer is F1** → nothing triggers. Pinned in Task 4.
5. **Profile named `Knight/PvP`, `con`-style or `default` when `Default` exists** → rejected with a clear message, no file created. Pinned in Task 8.

## Execution waves

Workers in the same wave run in parallel, each in its own git worktree branched from `main` at the start of the wave; the orchestrator merges each branch into `main` (resolving `AppHost.cs` wiring conflicts) before the next wave.

| Wave | Tasks | Depends on |
|---|---|---|
| 0 | Task 0 (orchestrator + user), Task 1 | — |
| 1 | Tasks 2, 3, 4, 5, 6 | 1 |
| 2 | Tasks 7, 8, 9 | 2–6 |
| 3 | Tasks 10, 11 | 7–9 |
| 4 | Task 12 | 10–11 |

## File map

```
TibiaMirror.sln
Directory.Build.props                      version, common props
src/TibiaMirror/TibiaMirror.csproj
src/TibiaMirror/app.manifest               PerMonitorV2 DPI
src/TibiaMirror/AppInfo.cs                 T1
src/TibiaMirror/App.xaml(.cs)              T1  startup, single instance, crash handler
src/TibiaMirror/AppHost.cs                 T1, wired by each task
src/TibiaMirror/AppPaths.cs                T1
src/TibiaMirror/Logging/Log.cs             T1
src/TibiaMirror/Shell/TrayIcon.cs          T1
src/TibiaMirror/Shell/SingleInstance.cs    T1
src/TibiaMirror/Models/*.cs                T1  (all models + PixelRect + Hotkey)
src/TibiaMirror/Abstractions/*.cs          T1  (IClock, ISoundPlayer, IWindowEnumerator, IKeyboardSource)
src/TibiaMirror/Views/MainWindow.xaml(.cs) T1  (tabs hosting the three views)
src/TibiaMirror/Views/MirrorsView.xaml     T1 stub → T7
src/TibiaMirror/Views/AudioTimersView.xaml T1 stub → T9
src/TibiaMirror/Views/ProfilesView.xaml    T1 stub → T8
src/TibiaMirror/Geometry/*.cs              T2
src/TibiaMirror/Persistence/*.cs           T3
src/TibiaMirror/Hotkeys/*.cs               T4  (+ Controls/HotkeyBox)
src/TibiaMirror/Interop/*.cs               T5
src/TibiaMirror/Detection/TibiaLocator.cs  T5
src/TibiaMirror/Audio/*.cs                 T6
src/TibiaMirror/Mirrors/*.cs               T7
src/TibiaMirror/Profiles/*.cs              T8
src/TibiaMirror/Timers/*.cs                T9
src/TibiaMirror/Settings/*.cs              T10
src/TibiaMirror/Countdown/*.cs             T11
tests/TibiaMirror.Tests/...                one test file per unit
installer/TibiaMirror.iss                  T12
.github/workflows/build.yml                T1
.github/workflows/release.yml              T12
README.md, LICENSE, CHANGELOG.md, docs/manual-test-checklist.md, docs/images/   T12 (LICENSE in T1)
```

---

### Task 0: Prerequisites (orchestrator + user, not a worker task)

- [ ] **Step 1:** Install the .NET 8 SDK: `winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements` (user approves the UAC prompt). Verify `dotnet --list-sdks` shows `8.0.x`.
- [ ] **Step 2:** Set the git identity the user chooses (repo-local): `git config user.name "<name>"`, `git config user.email "<email>"`.
- [ ] **Step 3:** `git init -b main` in `C:\Users\Ryan\Documents\TibiaMirror`, add `.gitignore` (standard VisualStudio .gitignore: `bin/ obj/ .vs/ *.user artifacts/ publish/`), commit spec + plan: `git commit -m "docs: add TibiaMirror design spec and plan"`.

---

### Task 1: Solution skeleton, contracts, app shell, CI build

**Files:**
- Create: `TibiaMirror.sln`, `Directory.Build.props`, `LICENSE`, `.github/workflows/build.yml`
- Create: `src/TibiaMirror/TibiaMirror.csproj`, `app.manifest`, `AppInfo.cs`, `AppPaths.cs`, `App.xaml`, `App.xaml.cs`, `AppHost.cs`, `Logging/Log.cs`, `Shell/TrayIcon.cs`, `Shell/SingleInstance.cs`
- Create: `src/TibiaMirror/Models/{PixelRect,Hotkey,RegionModel,CountdownSettings,AudioTimerModel,Profile,AppSettings}.cs`
- Create: `src/TibiaMirror/Abstractions/{IClock,ISoundPlayer,IWindowEnumerator,IKeyboardSource}.cs`
- Create: `src/TibiaMirror/Views/{MainWindow,MirrorsView,AudioTimersView,ProfilesView}.xaml(.cs)`
- Test: `tests/TibiaMirror.Tests/TibiaMirror.Tests.csproj`, `tests/TibiaMirror.Tests/Models/PixelRectTests.cs`, `tests/TibiaMirror.Tests/Models/HotkeyTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: every type below, exactly as written. Later tasks depend on these names.

- [ ] **Step 1: Create solution and projects**

```powershell
dotnet new sln -n TibiaMirror
dotnet new wpf -n TibiaMirror -o src/TibiaMirror -f net8.0
dotnet new xunit -n TibiaMirror.Tests -o tests/TibiaMirror.Tests -f net8.0
dotnet sln add src/TibiaMirror/TibiaMirror.csproj tests/TibiaMirror.Tests/TibiaMirror.Tests.csproj
dotnet add tests/TibiaMirror.Tests reference src/TibiaMirror
dotnet add src/TibiaMirror package CommunityToolkit.Mvvm --version 8.4.0
dotnet add src/TibiaMirror package NAudio --version 2.2.1
```

Delete the template `MainWindow.xaml(.cs)` and `UnitTest1.cs`; they are replaced below.

`Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <Version>0.1.0</Version>
    <Product>TibiaMirror</Product>
    <Company>TibiaMirror contributors</Company>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <WarningsAsErrors>nullable</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

`src/TibiaMirror/TibiaMirror.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms> <!-- NotifyIcon for tray -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AssemblyName>TibiaMirror</AssemblyName>
    <RootNamespace>TibiaMirror</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="TibiaMirror.Tests" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="NAudio" Version="2.2.1" />
  </ItemGroup>
</Project>
```
With `UseWindowsForms`, add `<Using Remove="System.Windows.Forms" />` and `<Using Remove="System.Drawing" />` to an ItemGroup to avoid ambiguous `Application`/`Point` types.

Test csproj: `TargetFramework` = `net8.0-windows`, `<UseWPF>true</UseWPF>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`.

`app.manifest`: standard manifest with
```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
  </windowsSettings>
</application>
```

- [ ] **Step 2: Write the failing model tests**

`tests/TibiaMirror.Tests/Models/PixelRectTests.cs`:
```csharp
using TibiaMirror.Models;
namespace TibiaMirror.Tests.Models;

public class PixelRectTests
{
    [Fact] public void RightAndBottom() { var r = new PixelRect(10, 20, 30, 40); Assert.Equal(40, r.Right); Assert.Equal(60, r.Bottom); }
    [Fact] public void Offset_moves() => Assert.Equal(new PixelRect(15, 17, 30, 40), new PixelRect(10, 20, 30, 40).Offset(5, -3));
    [Fact] public void Intersect_overlap() => Assert.Equal(new PixelRect(5, 5, 5, 5), new PixelRect(0, 0, 10, 10).Intersect(new PixelRect(5, 5, 10, 10)));
    [Fact] public void Intersect_disjoint_is_empty() => Assert.True(new PixelRect(0, 0, 10, 10).Intersect(new PixelRect(20, 20, 5, 5)).IsEmpty);
    [Fact] public void FromEdges_normalizes_reversed_drag() => Assert.Equal(new PixelRect(5, 5, 10, 10), PixelRect.FromCorners(15, 15, 5, 5));
}
```

`tests/TibiaMirror.Tests/Models/HotkeyTests.cs`:
```csharp
using TibiaMirror.Models;
namespace TibiaMirror.Tests.Models;

public class HotkeyTests
{
    [Fact] public void Display_plain_key() => Assert.Equal("F1", new Hotkey(0x70, HotkeyModifiers.None).ToString());
    [Fact] public void Display_with_modifiers_in_fixed_order() =>
        Assert.Equal("Ctrl+Shift+Alt+A", new Hotkey(0x41, HotkeyModifiers.Alt | HotkeyModifiers.Ctrl | HotkeyModifiers.Shift).ToString());
    [Fact] public void Equality_is_by_value() => Assert.Equal(new Hotkey(0x41, HotkeyModifiers.Ctrl), new Hotkey(0x41, HotkeyModifiers.Ctrl));
    [Fact] public void Json_roundtrip()
    {
        var h = new Hotkey(0x70, HotkeyModifiers.Ctrl);
        var json = System.Text.Json.JsonSerializer.Serialize(h);
        Assert.Equal(h, System.Text.Json.JsonSerializer.Deserialize<Hotkey>(json));
    }
}
```

- [ ] **Step 3: Run tests, verify they fail** — `dotnet test tests/TibiaMirror.Tests` → compile errors (types missing).

- [ ] **Step 4: Write the models and abstractions (exact contracts)**

`Models/PixelRect.cs`:
```csharp
namespace TibiaMirror.Models;

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public PixelRect Offset(int dx, int dy) => this with { X = X + dx, Y = Y + dy };
    public PixelRect Intersect(PixelRect o)
    {
        int l = Math.Max(X, o.X), t = Math.Max(Y, o.Y), r = Math.Min(Right, o.Right), b = Math.Min(Bottom, o.Bottom);
        return r <= l || b <= t ? default : new PixelRect(l, t, r - l, b - t);
    }
    public static PixelRect FromCorners(int x1, int y1, int x2, int y2) =>
        new(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));
}
```

`Models/Hotkey.cs`:
```csharp
namespace TibiaMirror.Models;

[Flags] public enum HotkeyModifiers { None = 0, Alt = 1, Ctrl = 2, Shift = 4, Win = 8 }

public sealed record Hotkey(int VirtualKey, HotkeyModifiers Modifiers)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(KeyName(VirtualKey));
        return string.Join("+", parts);
    }
    static string KeyName(int vk)
    {
        var key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(vk);
        return key switch
        {
            >= System.Windows.Input.Key.D0 and <= System.Windows.Input.Key.D9 => ((int)(key - System.Windows.Input.Key.D0)).ToString(),
            System.Windows.Input.Key.None => $"VK{vk:X2}",
            _ => key.ToString(),
        };
    }
}
```

`Models/CountdownSettings.cs`:
```csharp
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
```

`Models/RegionModel.cs`:
```csharp
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
}
```

`Models/AudioTimerModel.cs`:
```csharp
namespace TibiaMirror.Models;

public enum RepressBehavior { Restart, IgnoreWhileRunning }

public sealed class AudioTimerModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Timer";
    public Hotkey? Hotkey { get; set; }
    public double DurationSeconds { get; set; } = 10;
    public string Sound { get; set; } = "builtin:chime";
    public int Volume { get; set; } = 100;
    public RepressBehavior OnRepress { get; set; } = RepressBehavior.Restart;
}
```

`Models/Profile.cs`:
```csharp
namespace TibiaMirror.Models;

public sealed class Profile
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "Default";
    public List<RegionModel> Regions { get; set; } = new();
    public List<AudioTimerModel> Timers { get; set; } = new();
    public bool IsListening { get; set; }
    public bool StartListeningOnOpen { get; set; }
    public int MasterVolume { get; set; } = 80;
}
```

`Models/AppSettings.cs`:
```csharp
namespace TibiaMirror.Models;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public Hotkey? ShowHideAllHotkey { get; set; }
    public Hotkey? NextProfileHotkey { get; set; }
    public bool OnlyShowWhenTibiaFocused { get; set; }
    public string TibiaProcessName { get; set; } = "client";
    public string TibiaTitlePrefix { get; set; } = "Tibia";
    public bool TrayHintShown { get; set; }
    public PixelRect? MainWindowBounds { get; set; }
}
```

`Abstractions/IClock.cs`:
```csharp
namespace TibiaMirror.Abstractions;
public interface IClock { DateTime UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
```

`Abstractions/ISoundPlayer.cs`:
```csharp
namespace TibiaMirror.Abstractions;
public interface ISoundPlayer
{
    /// <param name="soundId">"builtin:&lt;id&gt;" or an absolute file path.</param>
    /// <param name="volume">0.0–1.0</param>
    void Play(string soundId, double volume);
    bool CanPlay(string soundId);
}
```

`Abstractions/IWindowEnumerator.cs`:
```csharp
using TibiaMirror.Models;
namespace TibiaMirror.Abstractions;

public sealed record WindowInfo(nint Handle, string Title, string ProcessName, bool IsVisible, bool IsMinimized,
    PixelRect WindowBounds, PixelRect ClientBounds); // both in screen physical px

public interface IWindowEnumerator
{
    IReadOnlyList<WindowInfo> GetTopLevelWindows();
    nint GetForegroundWindow();
    int GetWindowProcessId(nint hwnd);
}
```

`Abstractions/IKeyboardSource.cs`:
```csharp
using TibiaMirror.Models;
namespace TibiaMirror.Abstractions;

public readonly record struct KeyPress(int VirtualKey, HotkeyModifiers Modifiers);
public interface IKeyboardSource : IDisposable { event Action<KeyPress>? KeyDown; }
```

- [ ] **Step 5: Run tests, verify they pass** — `dotnet test tests/TibiaMirror.Tests` → 9 passed.

- [ ] **Step 6: App shell**

`AppInfo.cs`:
```csharp
namespace TibiaMirror;
public static class AppInfo
{
    public const string Name = "TibiaMirror";
    public const string DataFolderName = "TibiaMirror";
    public static string Version => typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
```

`AppPaths.cs`:
```csharp
namespace TibiaMirror;
public sealed class AppPaths
{
    public AppPaths(string root) { Root = root; Directory.CreateDirectory(ProfilesDir); Directory.CreateDirectory(LogsDir); }
    public static AppPaths Default() => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppInfo.DataFolderName));
    public string Root { get; }
    public string SettingsFile => Path.Combine(Root, "settings.json");
    public string ProfilesDir => Path.Combine(Root, "profiles");
    public string LastProfileFile => Path.Combine(Root, "last_profile");
    public string LogsDir => Path.Combine(Root, "logs");
    public string ProfileFile(string name) => Path.Combine(ProfilesDir, name + ".json");
}
```

`Logging/Log.cs`: static `Log.Init(string logsDir)`, `Log.Info(string)`, `Log.Warn(string)`, `Log.Error(string, Exception?)`; appends `yyyy-MM-dd HH:mm:ss.fff [LEVEL] message` lines to `logs/TibiaMirror-yyyyMMdd.log` under a lock; deletes log files older than 14 days on Init; never throws (wrap IO in try/catch).

`Shell/SingleInstance.cs`:
```csharp
namespace TibiaMirror.Shell;
public sealed class SingleInstance : IDisposable
{
    const string MutexName = @"Local\TibiaMirror.SingleInstance";
    const string EventName = @"Local\TibiaMirror.ShowExisting";
    readonly Mutex _mutex; readonly EventWaitHandle _show; RegisteredWaitHandle? _wait;
    public bool IsFirst { get; }
    public SingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out var created); IsFirst = created;
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
    }
    public void SignalFirstInstance() => _show.Set();
    public void ListenForShowRequests(Action onShow) =>
        _wait = ThreadPool.RegisterWaitForSingleObject(_show, (_, _) => onShow(), null, Timeout.Infinite, false);
    public void Dispose() { _wait?.Unregister(null); if (IsFirst) _mutex.ReleaseMutex(); _mutex.Dispose(); _show.Dispose(); }
}
```

`Shell/TrayIcon.cs`: wraps `System.Windows.Forms.NotifyIcon` (icon = `System.Drawing.SystemIcons.Application` until Task 12 adds the real icon), tooltip `AppInfo.Name`, context menu items **Show**, **Show/Hide mirrors**, **Exit**; double-click = Show. Exposes `event Action? ShowRequested, ToggleMirrorsRequested, ExitRequested;` and `void ShowBalloon(string title, string text)`. Disposes the NotifyIcon on Dispose.

`AppHost.cs` (composition root; later tasks add fields and wiring here):
```csharp
namespace TibiaMirror;
public sealed class AppHost : IDisposable
{
    public AppHost(AppPaths paths) { Paths = paths; }
    public AppPaths Paths { get; }
    public AppSettings Settings { get; set; } = new();
    public Profile CurrentProfile { get; set; } = new();
    public event Action? ToggleMirrorsRequested;   // raised by tray; MirrorService subscribes (Task 7)
    public void RequestToggleMirrors() => ToggleMirrorsRequested?.Invoke();
    public void Start() { }   // tasks add service startup here
    public void Dispose() { } // tasks add disposal here
}
```

`App.xaml`: no `StartupUri`; `ShutdownMode="OnExplicitShutdown"`. `App.xaml.cs`:
- `OnStartup`: create `SingleInstance`; if not first → `SignalFirstInstance()` and `Shutdown()`. Else `Log.Init`, hook `DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` → `Log.Error` + `MessageBox.Show("Something went wrong. Details were saved to the log folder:\n" + paths.LogsDir, AppInfo.Name)` (Dispatcher one sets `e.Handled = true`). Create `AppHost`, `MainWindow`, `TrayIcon`; `ListenForShowRequests(() => Dispatcher.Invoke(ShowMain))`; `host.Start()`; show main window.
- `ShowMain()`: `Show()`, `WindowState = Normal`, `Activate()`.
- Tray Exit → `host.Dispose()`, tray dispose, `Shutdown()`.

`Views/MainWindow.xaml`: `Title="TibiaMirror"`, 520×640, `MinWidth 420`. Top bar (`DockPanel.Dock=Top`): `TextBlock x:Name="StatusText"` ("⚪ Waiting for Tibia…"), `TextBlock x:Name="ProfileText"`, buttons `ShowHideAllButton` ("Show/Hide all"), `LockAllButton` ("Lock all"), `SettingsButton` ("⚙"). Body: `TabControl` with three `TabItem`s **Mirrors**, **Audio Timers**, **Profiles** containing `<views:MirrorsView/>`, `<views:AudioTimersView/>`, `<views:ProfilesView/>`. Closing (`OnClosing`) → `e.Cancel = true; Hide();` and, the first time (`!Settings.TrayHintShown`), `tray.ShowBalloon(AppInfo.Name, "Still running in the tray. Right-click the tray icon to exit.")` then set `TrayHintShown = true`. MainWindow constructor takes `(AppHost host, TrayIcon tray)`.

Each view stub (`MirrorsView`, `AudioTimersView`, `ProfilesView`) is a `UserControl` with a centered `TextBlock` "Coming soon".

- [ ] **Step 7: CI workflow** — `.github/workflows/build.yml`:
```yaml
name: build
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet build TibiaMirror.sln -c Release
      - run: dotnet test tests/TibiaMirror.Tests -c Release --no-build
```
`LICENSE`: MIT, `Copyright (c) 2026 TibiaMirror contributors`.

- [ ] **Step 8: Verify** — `dotnet build TibiaMirror.sln -c Debug` (0 errors), `dotnet test tests/TibiaMirror.Tests` (9 passed). Launch `src/TibiaMirror/bin/Debug/net8.0-windows/win-x64/TibiaMirror.exe`, confirm: window shows 3 tabs; closing hides to tray with balloon once; launching a second copy brings the first to front and exits; tray Exit ends the process.

- [ ] **Step 9: Commit** — `git add -A && git commit -m "feat: solution skeleton, contracts, app shell and CI build"`

---

### Task 2: Geometry (crop math, DPI, monitor clamp)

**Files:**
- Create: `src/TibiaMirror/Geometry/CropMath.cs`, `DpiMath.cs`, `MonitorClamp.cs`
- Test: `tests/TibiaMirror.Tests/Geometry/CropMathTests.cs`, `DpiMathTests.cs`, `MonitorClampTests.cs`

**Interfaces:**
- Consumes: `PixelRect` (Task 1).
- Produces:
  - `static PixelRect CropMath.FromSelection(PixelRect selectionOnScreen, PixelRect clientOnScreen)` — clamps to client, returns client-relative crop; returns `default` (empty) if smaller than 8×8 after clamping.
  - `static PixelRect CropMath.ToThumbnailSource(PixelRect crop, PixelRect windowOnScreen, PixelRect clientOnScreen)` — DWM `rcSource` is relative to the source window's top-left (window rect, incl. non-client area).
  - `static PixelRect CropMath.ResizeKeepingAspect(PixelRect current, int proposedWidth, int proposedHeight, double aspect)` — keeps X/Y, picks the dimension that changed more, min 8×8.
  - `static double DpiMath.Scale(uint dpi)` (= dpi/96.0), `static int DpiMath.ToPhysical(double dip, double scale)` (round), `static double DpiMath.ToDip(int px, double scale)`.
  - `static PixelRect MonitorClamp.Clamp(PixelRect rect, IReadOnlyList<PixelRect> workAreas, PixelRect primaryWorkArea)`.

- [ ] **Step 1: Write the failing tests**

`CropMathTests.cs`:
```csharp
using TibiaMirror.Geometry; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Geometry;

public class CropMathTests
{
    static readonly PixelRect Client = new(108, 131, 1600, 900);   // client area on screen
    static readonly PixelRect Window = new(100, 100, 1616, 939);   // window incl. borders/title

    [Fact] public void Selection_inside_client_becomes_client_relative() =>
        Assert.Equal(new PixelRect(92, 69, 300, 200), CropMath.FromSelection(new PixelRect(200, 200, 300, 200), Client));

    [Fact] public void Selection_partly_outside_is_clamped() =>
        Assert.Equal(new PixelRect(0, 0, 92, 69), CropMath.FromSelection(new PixelRect(0, 0, 200, 200), Client));

    [Fact] public void Tiny_selection_is_rejected() =>
        Assert.True(CropMath.FromSelection(new PixelRect(200, 200, 7, 50), Client).IsEmpty);

    [Fact] public void Thumbnail_source_adds_nonclient_offset() =>
        Assert.Equal(new PixelRect(100, 100, 300, 200), CropMath.ToThumbnailSource(new PixelRect(92, 69, 300, 200), Window, Client));

    [Fact] public void Thumbnail_source_unchanged_when_window_moves()   // Review Focus #1
    {
        var moved = 500;
        var src = CropMath.ToThumbnailSource(new PixelRect(92, 69, 300, 200), Window.Offset(moved, moved), Client.Offset(moved, moved));
        Assert.Equal(new PixelRect(100, 100, 300, 200), src);
    }

    [Fact] public void Crop_beyond_resized_client_is_clipped()        // Review Focus #1 (Tibia made smaller)
    {
        var smallClient = new PixelRect(108, 131, 200, 150);
        var smallWindow = new PixelRect(100, 100, 216, 189);
        Assert.Equal(new PixelRect(100, 100, 108, 81), CropMath.ToThumbnailSource(new PixelRect(92, 69, 300, 200), smallWindow, smallClient));
    }

    [Fact] public void Resize_keeps_aspect_using_larger_change() =>
        Assert.Equal(new PixelRect(10, 10, 400, 200), CropMath.ResizeKeepingAspect(new PixelRect(10, 10, 200, 100), 400, 120, 2.0));

    [Fact] public void Resize_never_below_minimum() =>
        Assert.Equal(new PixelRect(10, 10, 16, 8), CropMath.ResizeKeepingAspect(new PixelRect(10, 10, 200, 100), 2, 1, 2.0));
}
```

`DpiMathTests.cs`:
```csharp
using TibiaMirror.Geometry;
namespace TibiaMirror.Tests.Geometry;
public class DpiMathTests
{
    [Theory][InlineData(96u, 1.0)][InlineData(144u, 1.5)][InlineData(120u, 1.25)]
    public void Scale_from_dpi(uint dpi, double expected) => Assert.Equal(expected, DpiMath.Scale(dpi), 3);
    [Fact] public void Roundtrip_150_percent() { Assert.Equal(300, DpiMath.ToPhysical(200, 1.5)); Assert.Equal(200, DpiMath.ToDip(300, 1.5), 3); }
    [Fact] public void ToPhysical_rounds() => Assert.Equal(13, DpiMath.ToPhysical(10.2, 1.25));
}
```

`MonitorClampTests.cs`:
```csharp
using TibiaMirror.Geometry; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Geometry;

public class MonitorClampTests
{
    static readonly PixelRect Primary = new(0, 0, 1920, 1040);
    static readonly PixelRect Second = new(1920, 0, 2560, 1400);

    [Fact] public void Rect_fully_on_monitor_unchanged() =>
        Assert.Equal(new PixelRect(100, 100, 300, 200), MonitorClamp.Clamp(new PixelRect(100, 100, 300, 200), new[] { Primary, Second }, Primary));

    [Fact] public void Rect_on_disconnected_monitor_moves_to_primary()   // Review Focus #2
    {
        var r = MonitorClamp.Clamp(new PixelRect(5000, 300, 300, 200), new[] { Primary }, Primary);
        Assert.Equal(r, Primary.Intersect(r));   // entirely inside Primary
        Assert.Equal(300, r.Width); Assert.Equal(200, r.Height);
        Assert.True(r.X >= 0 && r.Right <= 1920 && r.Y >= 0 && r.Bottom <= 1040);
    }

    [Fact] public void Rect_mostly_offscreen_is_pulled_in_on_same_monitor()
    {
        var r = MonitorClamp.Clamp(new PixelRect(1900, 100, 300, 200), new[] { Primary }, Primary);
        Assert.Equal(new PixelRect(1620, 100, 300, 200), r);
    }

    [Fact] public void Rect_larger_than_monitor_is_shrunk_and_placed_at_origin() =>
        Assert.Equal(new PixelRect(0, 0, 1920, 1040), MonitorClamp.Clamp(new PixelRect(-50, -50, 4000, 3000), new[] { Primary }, Primary));
}
```

- [ ] **Step 2: Run, verify fail** — `dotnet test tests/TibiaMirror.Tests --filter FullyQualifiedName~Geometry` → compile errors.

- [ ] **Step 3: Implement**

```csharp
namespace TibiaMirror.Geometry;
using TibiaMirror.Models;

public static class CropMath
{
    public const int MinSize = 8;

    public static PixelRect FromSelection(PixelRect selectionOnScreen, PixelRect clientOnScreen)
    {
        var clipped = selectionOnScreen.Intersect(clientOnScreen);
        if (clipped.Width < MinSize || clipped.Height < MinSize) return default;
        return clipped.Offset(-clientOnScreen.X, -clientOnScreen.Y);
    }

    public static PixelRect ToThumbnailSource(PixelRect crop, PixelRect windowOnScreen, PixelRect clientOnScreen)
    {
        var clientRelative = new PixelRect(0, 0, clientOnScreen.Width, clientOnScreen.Height);
        var visible = crop.Intersect(clientRelative);
        return visible.Offset(clientOnScreen.X - windowOnScreen.X, clientOnScreen.Y - windowOnScreen.Y);
    }

    public static PixelRect ResizeKeepingAspect(PixelRect current, int proposedWidth, int proposedHeight, double aspect)
    {
        double dw = Math.Abs(proposedWidth - current.Width) / (double)Math.Max(1, current.Width);
        double dh = Math.Abs(proposedHeight - current.Height) / (double)Math.Max(1, current.Height);
        int w, h;
        if (dw >= dh) { w = proposedWidth; h = (int)Math.Round(w / aspect); }
        else { h = proposedHeight; w = (int)Math.Round(h * aspect); }
        if (h < MinSize) { h = MinSize; w = (int)Math.Round(h * aspect); }
        if (w < MinSize) { w = MinSize; h = (int)Math.Round(w / aspect); }
        return current with { Width = w, Height = h };
    }
}
```
`DpiMath`: `Scale(uint dpi) => dpi / 96.0`; `ToPhysical(double dip, double scale) => (int)Math.Round(dip * scale, MidpointRounding.AwayFromZero)`; `ToDip(int px, double scale) => px / scale`.

`MonitorClamp.Clamp`: (1) pick the work area with the largest intersection area with `rect`; if none intersects at all (`Intersect(...).IsEmpty` for every area), use `primaryWorkArea` and start from `rect` moved to `(primary.X + 40, primary.Y + 40)`; (2) shrink width/height to at most the chosen area's size; (3) shift X into `[area.X, area.Right - width]` and Y into `[area.Y, area.Bottom - height]`. For the "larger than monitor" test, step 1 picks Primary (largest intersection), step 2 shrinks to 1920×1040, step 3 places at 0,0.

- [ ] **Step 4: Run, verify pass** — all Geometry tests green, full suite green.
- [ ] **Step 5: Commit** — `git commit -am "feat: geometry helpers for crops, DPI and monitor clamping"` (use `git add` for new files first).

---

### Task 3: Persistence (atomic JSON store with backup recovery)

**Files:**
- Create: `src/TibiaMirror/Persistence/JsonStore.cs`, `LoadResult.cs`, `Debouncer.cs`
- Test: `tests/TibiaMirror.Tests/Persistence/JsonStoreTests.cs`, `DebouncerTests.cs`

**Interfaces:**
- Consumes: models (Task 1).
- Produces:
  - `enum LoadStatus { Ok, Missing, RestoredFromBackup, Corrupt }`
  - `sealed record LoadResult<T>(T Value, LoadStatus Status, string? CorruptPath)`
  - `static class JsonStore { static JsonSerializerOptions Options; static LoadResult<T> Load<T>(string path, Func<T> createDefault) where T : class; static void Save<T>(string path, T value); }`
  - `sealed class Debouncer(TimeSpan delay, Action action) : IDisposable { void Trigger(); void Flush(); }` — `Trigger` restarts a timer; `Flush` runs the pending action immediately (if pending) on the caller thread.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Models; using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Persistence;

public class JsonStoreTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("tm-tests").FullName;
    string P(string n) => Path.Combine(_dir, n);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact] public void Missing_file_returns_default()
    {
        var r = JsonStore.Load(P("a.json"), () => new Profile { Name = "Default" });
        Assert.Equal(LoadStatus.Missing, r.Status); Assert.Equal("Default", r.Value.Name);
    }

    [Fact] public void Roundtrip_preserves_regions_hotkeys_and_enums()
    {
        var p = new Profile { Name = "Knight" };
        p.Regions.Add(new RegionModel { Name = "HP", Crop = new(1, 2, 30, 40), Mirror = new(5, 6, 70, 80),
            Countdown = new CountdownSettings { Hotkey = new Hotkey(0x70, HotkeyModifiers.Ctrl), Side = BarSide.Left } });
        p.Timers.Add(new AudioTimerModel { Name = "Haste", OnRepress = RepressBehavior.IgnoreWhileRunning });
        JsonStore.Save(P("k.json"), p);
        var back = JsonStore.Load(P("k.json"), () => new Profile()).Value;
        Assert.Equal(new PixelRect(1, 2, 30, 40), back.Regions[0].Crop);
        Assert.Equal(new Hotkey(0x70, HotkeyModifiers.Ctrl), back.Regions[0].Countdown.Hotkey);
        Assert.Equal(BarSide.Left, back.Regions[0].Countdown.Side);
        Assert.Equal(RepressBehavior.IgnoreWhileRunning, back.Timers[0].OnRepress);
        Assert.Contains("\"Left\"", File.ReadAllText(P("k.json")));   // enums as strings, human-readable
    }

    [Fact] public void Second_save_keeps_previous_as_bak()
    {
        JsonStore.Save(P("s.json"), new Profile { Name = "One" });
        JsonStore.Save(P("s.json"), new Profile { Name = "Two" });
        Assert.Contains("One", File.ReadAllText(P("s.json.bak")));
    }

    [Fact] public void Corrupt_file_restores_from_backup()               // Review Focus #3
    {
        JsonStore.Save(P("c.json"), new Profile { Name = "Good" });
        JsonStore.Save(P("c.json"), new Profile { Name = "Good2" });
        File.WriteAllText(P("c.json"), "{ not json");
        var r = JsonStore.Load(P("c.json"), () => new Profile());
        Assert.Equal(LoadStatus.RestoredFromBackup, r.Status); Assert.Equal("Good", r.Value.Name);
    }

    [Fact] public void Corrupt_without_backup_is_quarantined()          // Review Focus #3
    {
        File.WriteAllText(P("x.json"), "garbage");
        var r = JsonStore.Load(P("x.json"), () => new Profile { Name = "Fresh" });
        Assert.Equal(LoadStatus.Corrupt, r.Status); Assert.Equal("Fresh", r.Value.Name);
        Assert.False(File.Exists(P("x.json"))); Assert.True(File.Exists(r.CorruptPath));
    }

    [Fact] public void Empty_file_counts_as_corrupt()
    {
        File.WriteAllText(P("e.json"), "");
        Assert.Equal(LoadStatus.Corrupt, JsonStore.Load(P("e.json"), () => new Profile()).Status);
    }

    [Fact] public void Leftover_tmp_file_does_not_break_save()
    {
        File.WriteAllText(P("t.json.tmp"), "junk");
        JsonStore.Save(P("t.json"), new Profile { Name = "T" });
        Assert.Equal("T", JsonStore.Load(P("t.json"), () => new Profile()).Value.Name);
    }
}
```

`DebouncerTests.cs`:
```csharp
using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Persistence;
public class DebouncerTests
{
    [Fact] public void Many_triggers_run_once_after_delay()
    {
        int n = 0; using var d = new Debouncer(TimeSpan.FromMilliseconds(100), () => Interlocked.Increment(ref n));
        for (int i = 0; i < 5; i++) d.Trigger();
        Thread.Sleep(400); Assert.Equal(1, n);
    }
    [Fact] public void Flush_runs_pending_immediately_and_only_once()
    {
        int n = 0; using var d = new Debouncer(TimeSpan.FromSeconds(10), () => n++);
        d.Trigger(); d.Flush(); d.Flush(); Assert.Equal(1, n);
    }
    [Fact] public void Flush_without_trigger_does_nothing()
    {
        int n = 0; using var d = new Debouncer(TimeSpan.FromSeconds(10), () => n++); d.Flush(); Assert.Equal(0, n);
    }
}
```

- [ ] **Step 2: Run, verify fail.**

- [ ] **Step 3: Implement**

```csharp
using System.Text.Json; using System.Text.Json.Serialization;
namespace TibiaMirror.Persistence;

public enum LoadStatus { Ok, Missing, RestoredFromBackup, Corrupt }
public sealed record LoadResult<T>(T Value, LoadStatus Status, string? CorruptPath);

public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static LoadResult<T> Load<T>(string path, Func<T> createDefault) where T : class
    {
        if (!File.Exists(path))
            return File.Exists(path + ".bak") && TryRead<T>(path + ".bak", out var b)
                ? new(b!, LoadStatus.RestoredFromBackup, null)
                : new(createDefault(), LoadStatus.Missing, null);
        if (TryRead<T>(path, out var v)) return new(v!, LoadStatus.Ok, null);
        var corrupt = path + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".corrupt";
        try { File.Move(path, corrupt, overwrite: true); } catch (IOException) { corrupt = path; }
        if (File.Exists(path + ".bak") && TryRead<T>(path + ".bak", out var bak))
            return new(bak!, LoadStatus.RestoredFromBackup, corrupt);
        return new(createDefault(), LoadStatus.Corrupt, corrupt);
    }

    static bool TryRead<T>(string path, out T? value) where T : class
    {
        try { value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options); return value is not null; }
        catch (Exception e) when (e is JsonException or IOException or NotSupportedException) { value = null; return false; }
    }

    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, Options));
        if (File.Exists(path)) File.Replace(tmp, path, path + ".bak", ignoreMetadataErrors: true);
        else File.Move(tmp, path);
    }
}
```
`Debouncer`: uses `System.Threading.Timer`; `Trigger()` sets `_pending = true` and `_timer.Change(delay, Infinite)`; timer callback and `Flush()` both call `RunIfPending()` which, under a lock, runs the action only if `_pending` and clears it. Callers that need the UI thread marshal inside their action.

- [ ] **Step 4: Run, verify pass.**
- [ ] **Step 5: Commit** — `git commit -m "feat: atomic JSON store with backup recovery and debouncer"`

---

### Task 4: Hotkeys (keyboard hook, matching, conflicts, recorder control)

**Files:**
- Create: `src/TibiaMirror/Hotkeys/KeyboardHook.cs`, `HotkeyService.cs`, `HotkeyScope.cs`
- Create: `src/TibiaMirror/Controls/HotkeyBox.xaml(.cs)`
- Test: `tests/TibiaMirror.Tests/Hotkeys/HotkeyServiceTests.cs`

**Interfaces:**
- Consumes: `Hotkey`, `HotkeyModifiers`, `KeyPress`, `IKeyboardSource` (Task 1).
- Produces:
  - `enum HotkeyScope { Global, WhenTibiaFocused }`
  - `sealed class KeyboardHook : IKeyboardSource` — installs `WH_KEYBOARD_LL` on construction (must be created on the UI thread); raises `KeyDown` for `WM_KEYDOWN`/`WM_SYSKEYDOWN`, ignoring the modifier keys themselves (VK 0x10–0x12, 0xA0–0xA5, 0x5B, 0x5C) and auto-repeat (track keys currently down; raise only on first down; clear on `WM_KEYUP`/`WM_SYSKEYUP`). Modifiers read via `GetKeyState` (high bit). Always returns `CallNextHookEx(...)`. Throws `InvalidOperationException` if `SetWindowsHookEx` returns 0.
  - `sealed class HotkeyService(IKeyboardSource source, Func<bool> isTibiaFocused, Action<Action> dispatch) : IDisposable`
    - `IDisposable Register(object owner, Hotkey hotkey, HotkeyScope scope, Action callback)`
    - `IReadOnlyList<object> FindConflicts(Hotkey hotkey, object? excludeOwner)`
    - `bool IsSuspended { get; set; }` — true while a `HotkeyBox` is recording (no callbacks fire).
  - `HotkeyBox` control: `DependencyProperty Hotkey (Hotkey?)`, two-way bindable; click → "Press a key…" and records next key-down (Esc cancels, Backspace/Delete clears); raises routed event `HotkeyChanged`. Static `HotkeyBox.Service` (set by AppHost) so it can set `IsSuspended` while recording.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Abstractions; using TibiaMirror.Hotkeys; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Hotkeys;

sealed class FakeKeyboard : IKeyboardSource
{
    public event Action<KeyPress>? KeyDown;
    public void Press(int vk, HotkeyModifiers m = HotkeyModifiers.None) => KeyDown?.Invoke(new KeyPress(vk, m));
    public void Dispose() { }
}

public class HotkeyServiceTests
{
    readonly FakeKeyboard _kb = new();
    bool _tibiaFocused = true;
    HotkeyService Svc() => new(_kb, () => _tibiaFocused, a => a());
    const int F1 = 0x70;

    [Fact] public void Matching_key_fires()
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.None), HotkeyScope.WhenTibiaFocused, () => n++); _kb.Press(F1); Assert.Equal(1, n); }

    [Fact] public void Different_modifiers_do_not_fire()                    // Review Focus #4
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.None), HotkeyScope.WhenTibiaFocused, () => n++); _kb.Press(F1, HotkeyModifiers.Ctrl); Assert.Equal(0, n); }

    [Fact] public void Tibia_scope_ignored_when_other_app_focused()       // Review Focus #4
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.None), HotkeyScope.WhenTibiaFocused, () => n++); _tibiaFocused = false; _kb.Press(F1); Assert.Equal(0, n); }

    [Fact] public void Global_scope_fires_regardless_of_focus()
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.Ctrl), HotkeyScope.Global, () => n++); _tibiaFocused = false; _kb.Press(F1, HotkeyModifiers.Ctrl); Assert.Equal(1, n); }

    [Fact] public void Disposing_registration_stops_it()
    { int n = 0; using var s = Svc(); var r = s.Register(this, new Hotkey(F1, 0), HotkeyScope.Global, () => n++); r.Dispose(); _kb.Press(F1); Assert.Equal(0, n); }

    [Fact] public void Two_owners_same_key_both_fire()
    { int n = 0; using var s = Svc(); s.Register("a", new Hotkey(F1, 0), HotkeyScope.Global, () => n++); s.Register("b", new Hotkey(F1, 0), HotkeyScope.Global, () => n++); _kb.Press(F1); Assert.Equal(2, n); }

    [Fact] public void Suspended_service_fires_nothing()
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, 0), HotkeyScope.Global, () => n++); s.IsSuspended = true; _kb.Press(F1); Assert.Equal(0, n); }

    [Fact] public void Conflicts_exclude_own_owner()
    {
        using var s = Svc(); s.Register("a", new Hotkey(F1, 0), HotkeyScope.Global, () => { }); s.Register("b", new Hotkey(F1, 0), HotkeyScope.Global, () => { });
        Assert.Equal(new object[] { "b" }, s.FindConflicts(new Hotkey(F1, 0), "a"));
        Assert.Empty(s.FindConflicts(new Hotkey(F1, HotkeyModifiers.Shift), null));
    }

    [Fact] public void Callback_exception_does_not_stop_other_callbacks()
    { int n = 0; using var s = Svc(); s.Register("a", new Hotkey(F1, 0), HotkeyScope.Global, () => throw new Exception("x")); s.Register("b", new Hotkey(F1, 0), HotkeyScope.Global, () => n++); _kb.Press(F1); Assert.Equal(1, n); }
}
```

- [ ] **Step 2: Run, verify fail.**

- [ ] **Step 3: Implement**

`HotkeyService`: keeps `List<Registration>` (record with Owner, Hotkey, Scope, Callback) under a lock. On `KeyDown(press)`: if `IsSuspended` return; snapshot matches where `Hotkey.VirtualKey == press.VirtualKey && Hotkey.Modifiers == press.Modifiers`; if any match has scope `WhenTibiaFocused`, evaluate `isTibiaFocused()` once; call `dispatch(() => { foreach match: try { cb(); } catch (Exception e) { Log.Error("Hotkey callback failed", e); } })` — the try/catch is per callback. `Register` returns an `IDisposable` that removes that registration. `Dispose` unsubscribes from the source.

`KeyboardHook` P/Invoke (keep in this file; Task 5 owns the rest of `Interop/`):
```csharp
[DllImport("user32.dll", SetLastError = true)] static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc fn, nint hMod, uint threadId);
[DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(nint hhk);
[DllImport("user32.dll")] static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);
[DllImport("user32.dll")] static extern short GetKeyState(int vk);
[DllImport("kernel32.dll")] static extern nint GetModuleHandle(string? name);
delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);
const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
```
Store the delegate in a field (prevent GC). Read `vkCode` as `Marshal.ReadInt32(lParam)`. Raise `KeyDown` inside try/catch, then `return CallNextHookEx(_hook, nCode, wParam, lParam);` unconditionally. Modifier detection: `(GetKeyState(0x11) & 0x8000) != 0` → Ctrl; 0x10 Shift; 0x12 Alt; 0x5B/0x5C Win.

`HotkeyBox`: a `Button`-styled `UserControl` showing `Hotkey?.ToString() ?? "None"`. On click: text "Press a key…", set `HotkeyBox.Service.IsSuspended = true`, subscribe to the service's source via a `static IKeyboardSource? Source` (set by AppHost); on next KeyDown (dispatch to UI): Esc → cancel; Back/Delete → `Hotkey = null`; else `Hotkey = new Hotkey(vk, mods)`; unsubscribe; `IsSuspended = false`; raise `HotkeyChanged`. Losing focus cancels recording.

Wire in `AppHost.Start()`: `Keyboard = new KeyboardHook(); Hotkeys = new HotkeyService(Keyboard, () => IsTibiaFocused(), a => Application.Current.Dispatcher.BeginInvoke(a)); HotkeyBox.Service = Hotkeys; HotkeyBox.Source = Keyboard;` where `IsTibiaFocused()` is a `public Func<bool> IsTibiaFocused { get; set; } = () => false;` on AppHost (Task 5 assigns it). Wrap hook creation in try/catch → on failure `Log.Error` and set `public bool HotkeysUnavailable` (Task 10 shows the banner).

- [ ] **Step 4: Run, verify pass.** (End-to-end hook behavior is checked manually in Task 9.)
- [ ] **Step 5: Commit** — `git commit -m "feat: observing keyboard hook, hotkey service and recorder control"`

---

### Task 5: Win32 interop and Tibia detection

**Files:**
- Create: `src/TibiaMirror/Interop/NativeMethods.cs`, `Win32WindowEnumerator.cs`, `DwmThumbnail.cs`, `WindowStyles.cs`, `Monitors.cs`
- Create: `src/TibiaMirror/Detection/TibiaLocator.cs`, `TibiaState.cs`
- Test: `tests/TibiaMirror.Tests/Detection/TibiaLocatorTests.cs`

**Interfaces:**
- Consumes: `IWindowEnumerator`, `WindowInfo`, `AppSettings`, `PixelRect` (Task 1).
- Produces:
  - `sealed class Win32WindowEnumerator : IWindowEnumerator` (EnumWindows + GetWindowText + GetWindowThreadProcessId + `Process.GetProcessById(pid).ProcessName` cached per pid + IsWindowVisible + IsIconic + GetWindowRect + GetClientRect/ClientToScreen).
  - `sealed class DwmThumbnail : IDisposable` — `DwmThumbnail(nint destinationHwnd, nint sourceHwnd)` (throws `Win32Exception` on failure HRESULT); `void Update(PixelRect source, PixelRect destination, double opacity, bool visible)` (destination is relative to the destination window's client area, physical px); `Dispose()` unregisters.
  - `static class WindowStyles { void MakeToolWindow(nint hwnd); void SetClickThrough(nint hwnd, bool enabled); void SetBoundsPhysical(nint hwnd, PixelRect rect); PixelRect GetBoundsPhysical(nint hwnd); }` — `MakeToolWindow` adds `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`; click-through toggles `WS_EX_TRANSPARENT | WS_EX_LAYERED`; `SetBoundsPhysical` uses `SetWindowPos(hwnd, HWND_TOPMOST, x, y, w, h, SWP_NOACTIVATE)`.
  - `static class Monitors { IReadOnlyList<PixelRect> WorkAreas(); PixelRect PrimaryWorkArea(); uint DpiForWindow(nint hwnd); }` via `EnumDisplayMonitors` + `GetMonitorInfo` + `GetDpiForWindow`.
  - `sealed record TibiaState(WindowInfo? Window, bool IsFocused) { bool IsFound => Window is not null; bool IsMinimized => Window?.IsMinimized ?? false; }`
  - `sealed class TibiaLocator(IWindowEnumerator windows, Func<AppSettings> settings, Func<nint, bool> isOwnWindow)` — `TibiaState State { get; }`, `event Action<TibiaState>? StateChanged;` (raised only when any of: handle, IsMinimized, IsFocused, WindowBounds, ClientBounds changed), `void Poll()`.
  - `AppHost` gets `public TibiaLocator Locator`, a 500 ms `DispatcherTimer` calling `Locator.Poll()`, and `IsTibiaFocused = () => Locator.State.IsFocused`.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Abstractions; using TibiaMirror.Detection; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Detection;

sealed class FakeWindows : IWindowEnumerator
{
    public List<WindowInfo> Windows = new(); public nint Foreground; public Dictionary<nint, int> Pids = new();
    public IReadOnlyList<WindowInfo> GetTopLevelWindows() => Windows;
    public nint GetForegroundWindow() => Foreground;
    public int GetWindowProcessId(nint h) => Pids.TryGetValue(h, out var p) ? p : 0;
}

public class TibiaLocatorTests
{
    static WindowInfo Win(nint h, string title, string proc, bool visible = true, bool min = false) =>
        new(h, title, proc, visible, min, new PixelRect(0, 0, 800, 600), new PixelRect(8, 31, 784, 561));
    readonly FakeWindows _w = new();
    readonly AppSettings _s = new();
    TibiaLocator Make(Func<nint, bool>? own = null) => new(_w, () => _s, own ?? (_ => false));

    [Fact] public void Finds_tibia_client_by_process_and_title()
    {
        _w.Windows.Add(Win(1, "Tibia - Knight Bob", "client"));
        var l = Make(); l.Poll(); Assert.Equal((nint?)1, l.State.Window?.Handle);
    }
    [Fact] public void Ignores_wrong_process_with_tibia_title() { _w.Windows.Add(Win(1, "Tibia wiki - Chrome", "chrome")); var l = Make(); l.Poll(); Assert.False(l.State.IsFound); }
    [Fact] public void Ignores_launcher_and_invisible_windows()
    {
        _w.Windows.Add(Win(1, "Tibia Launcher", "client_launcher")); _w.Windows.Add(Win(2, "Tibia", "client", visible: false));
        var l = Make(); l.Poll(); Assert.False(l.State.IsFound);
    }
    [Fact] public void Process_match_is_case_insensitive_and_configurable()
    {
        _s.TibiaProcessName = "MyClient"; _w.Windows.Add(Win(1, "Tibia", "myclient"));
        var l = Make(); l.Poll(); Assert.True(l.State.IsFound);
    }
    [Fact] public void Raises_lost_when_window_disappears()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(); l.Poll();
        TibiaState? last = null; l.StateChanged += s => last = s;
        _w.Windows.Clear(); l.Poll(); Assert.False(last!.IsFound);
    }
    [Fact] public void Focus_true_when_tibia_or_own_window_foreground()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(h => h == 99);
        _w.Foreground = 1; l.Poll(); Assert.True(l.State.IsFocused);
        _w.Foreground = 99; l.Poll(); Assert.True(l.State.IsFocused);
        _w.Foreground = 5; l.Poll(); Assert.False(l.State.IsFocused);
    }
    [Fact] public void No_event_when_nothing_changed()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(); l.Poll();
        int n = 0; l.StateChanged += _ => n++; l.Poll(); l.Poll(); Assert.Equal(0, n);
    }
    [Fact] public void Event_when_window_moves()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(); l.Poll();
        int n = 0; l.StateChanged += _ => n++;
        _w.Windows[0] = _w.Windows[0] with { WindowBounds = new(50, 50, 800, 600), ClientBounds = new(58, 81, 784, 561) };
        l.Poll(); Assert.Equal(1, n);
    }
    [Fact] public void Minimized_is_reported()
    {
        _w.Windows.Add(Win(1, "Tibia", "client", min: true)); var l = Make(); l.Poll(); Assert.True(l.State.IsMinimized);
    }
}
```

- [ ] **Step 2: Run, verify fail.**

- [ ] **Step 3: Implement**

`TibiaLocator.Poll()`: read settings; `match = windows.GetTopLevelWindows().FirstOrDefault(w => w.IsVisible && string.Equals(w.ProcessName, s.TibiaProcessName, OrdinalIgnoreCase) && w.Title.StartsWith(s.TibiaTitlePrefix, Ordinal))`; `fg = windows.GetForegroundWindow()`; `focused = match is not null && (fg == match.Handle || isOwnWindow(fg))`; build new `TibiaState`; compare with previous (record equality covers handle, bounds, minimized, focus); if different, set and raise.

`isOwnWindow` in AppHost: `h => windows.GetWindowProcessId(h) == Environment.ProcessId`.

`NativeMethods.cs` declares (all `user32` unless noted): `EnumWindows`, `GetWindowText`, `GetWindowTextLength`, `GetWindowThreadProcessId`, `IsWindowVisible`, `IsIconic`, `GetWindowRect`, `GetClientRect`, `ClientToScreen`, `GetForegroundWindow`, `GetWindowLongPtr`, `SetWindowLongPtr`, `SetWindowPos`, `GetDpiForWindow`, `EnumDisplayMonitors`, `GetMonitorInfo`, `MonitorFromWindow`; `dwmapi`: `DwmRegisterThumbnail`, `DwmUnregisterThumbnail`, `DwmUpdateThumbnailProperties`, `DwmQueryThumbnailSourceSize`; structs `RECT`, `POINT`, `MONITORINFO`, `DWM_THUMBNAIL_PROPERTIES` (`dwFlags`, `rcDestination`, `rcSource`, `opacity` (byte), `fVisible`, `fSourceClientAreaOnly`); constants `DWM_TNP_RECTDESTINATION=1, DWM_TNP_RECTSOURCE=2, DWM_TNP_OPACITY=4, DWM_TNP_VISIBLE=8, DWM_TNP_SOURCECLIENTAREAONLY=0x10`.

`DwmThumbnail.Update`: flags = all five; `fSourceClientAreaOnly = false` (source rect already includes the non-client offset from `CropMath.ToThumbnailSource`); `opacity = (byte)Math.Round(Math.Clamp(opacity, 0.1, 1) * 255)`.

The enumerator skips windows with empty titles and never throws for exited processes (catch `ArgumentException`/`InvalidOperationException` → process name `""`).

Main window status: in `AppHost.Start()` subscribe `Locator.StateChanged` and expose `event Action<TibiaState>? TibiaStateChanged`; `MainWindow` sets `StatusText` to "🟢 Tibia detected", "🟡 Tibia minimized" or "⚪ Waiting for Tibia…".

- [ ] **Step 4: Run, verify pass.** Manual: run app, start Tibia → status turns 🟢 within 1 s; minimize Tibia → 🟡; close Tibia → ⚪.
- [ ] **Step 5: Commit** — `git commit -m "feat: win32 interop, DWM thumbnail wrapper and Tibia detection"`

---

### Task 6: Audio (built-in synthesized sounds, NAudio player)

**Files:**
- Create: `src/TibiaMirror/Audio/BuiltInSounds.cs`, `SoundPlayer.cs`
- Test: `tests/TibiaMirror.Tests/Audio/BuiltInSoundsTests.cs`, `SoundPlayerTests.cs`

**Interfaces:**
- Consumes: `ISoundPlayer` (Task 1).
- Produces:
  - `static class BuiltInSounds { IReadOnlyList<(string Id, string DisplayName)> All; byte[] RenderWav(string id); const string DefaultId = "builtin:chime"; }` — ids: `builtin:chime`, `builtin:ding`, `builtin:double-ding`, `builtin:alert`, `builtin:low-beep`, `builtin:rise`.
  - `sealed class SoundPlayer : ISoundPlayer, IDisposable` — plays via `WaveOutEvent` + `WaveFileReader`/`AudioFileReader`; multiple sounds may overlap (one output per play, disposed on `PlaybackStopped`). Unknown/missing/unplayable id → plays `DefaultId` and logs a warning. `CanPlay(id)`: built-in id known, or file exists with extension `.mp3`/`.wav`.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Audio;
namespace TibiaMirror.Tests.Audio;

public class BuiltInSoundsTests
{
    [Fact] public void Six_sounds_with_unique_ids() { Assert.Equal(6, BuiltInSounds.All.Count); Assert.Equal(6, BuiltInSounds.All.Select(s => s.Id).Distinct().Count()); }

    [Theory][MemberData(nameof(Ids))]
    public void Each_renders_valid_short_pcm_wav(string id)
    {
        var wav = BuiltInSounds.RenderWav(id);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(wav, 8, 4));
        using var r = new NAudio.Wave.WaveFileReader(new MemoryStream(wav));
        Assert.Equal(44100, r.WaveFormat.SampleRate);
        Assert.InRange(r.TotalTime.TotalSeconds, 0.1, 1.5);
    }
    public static IEnumerable<object[]> Ids() => BuiltInSounds.All.Select(s => new object[] { s.Id });

    [Fact] public void Unknown_id_throws() => Assert.Throws<ArgumentException>(() => BuiltInSounds.RenderWav("builtin:nope"));
}

public class SoundPlayerTests
{
    [Fact] public void CanPlay_rules()
    {
        using var p = new SoundPlayer();
        Assert.True(p.CanPlay("builtin:chime"));
        Assert.False(p.CanPlay("builtin:nope"));
        Assert.False(p.CanPlay(@"C:\definitely\missing.mp3"));
        var txt = Path.GetTempFileName(); try { Assert.False(p.CanPlay(txt)); } finally { File.Delete(txt); }
    }
}
```

- [ ] **Step 2: Run, verify fail.**

- [ ] **Step 3: Implement** `BuiltInSounds.RenderWav`: 44.1 kHz mono 16-bit; each sound is a list of `(freqHz, startSec, durSec)` sine notes with a 5 ms attack and exponential decay `exp(-t*6)`, summed and normalized to 0.8 peak, written with `NAudio.Wave.WaveFileWriter` into a `MemoryStream`. Note recipes:
  - chime: (1046.5, 0, 0.6), (1568, 0.0, 0.6) at half amplitude
  - ding: (1318.5, 0, 0.4)
  - double-ding: (1318.5, 0, 0.25), (1318.5, 0.3, 0.25)
  - alert: (880, 0, 0.12), (880, 0.18, 0.12), (880, 0.36, 0.12)
  - low-beep: (440, 0, 0.35)
  - rise: (523.25, 0, 0.15), (659.25, 0.15, 0.15), (783.99, 0.3, 0.3)
  Display names: "Chime", "Ding", "Double ding", "Alert", "Low beep", "Rise". Cache rendered bytes in a `ConcurrentDictionary`.

`SoundPlayer.Play`: resolve stream (`new WaveFileReader(new MemoryStream(BuiltInSounds.RenderWav(id)))` or `new AudioFileReader(path)`), wrap in `NAudio.Wave.SampleProviders.VolumeSampleProvider` with `Volume = (float)Math.Clamp(volume, 0, 1)`, `new WaveOutEvent()`, `Init`, `Play`; dispose output + reader on `PlaybackStopped`. Any exception → `Log.Warn` and, if not already the default, retry with `DefaultId`.

Wire in `AppHost`: `public ISoundPlayer Sounds { get; } = new SoundPlayer();` disposed in `Dispose()`.

- [ ] **Step 4: Run, verify pass.**
- [ ] **Step 5: Commit** — `git commit -m "feat: synthesized built-in sounds and NAudio sound player"`

---

### Task 7: Mirror regions (selector, mirror windows, service, Mirrors tab)

**Files:**
- Create: `src/TibiaMirror/Mirrors/MirrorWindow.xaml(.cs)`, `RegionSelectorWindow.xaml(.cs)`, `MirrorService.cs`, `MirrorCardViewModel.cs`, `MirrorsViewModel.cs`, `RenameDialog.xaml(.cs)`
- Modify: `src/TibiaMirror/Views/MirrorsView.xaml(.cs)`, `src/TibiaMirror/AppHost.cs`, `src/TibiaMirror/Views/MainWindow.xaml.cs` (Show/Hide all, Lock all buttons)
- Test: `tests/TibiaMirror.Tests/Mirrors/MirrorVisibilityTests.cs`

**Interfaces:**
- Consumes: `PixelRect`, `RegionModel`, `Profile`, `AppSettings` (T1); `CropMath`, `MonitorClamp`, `DpiMath` (T2); `DwmThumbnail`, `WindowStyles`, `Monitors`, `TibiaLocator`, `TibiaState` (T5); `HotkeyService`, `HotkeyScope` (T4).
- Produces:
  - `static class MirrorVisibility { static bool ShouldShow(RegionModel r, TibiaState tibia, bool allHidden, bool onlyWhenFocused) }` — true iff `r.IsVisible && !allHidden && tibia.IsFound && !tibia.IsMinimized && (!onlyWhenFocused || tibia.IsFocused)`.
  - `sealed class MirrorService : IDisposable`
    - ctor `(TibiaLocator locator, Func<AppSettings> settings, Action onChanged)` — `onChanged` is called after any user edit (used for autosave in Task 10).
    - `void Load(Profile profile)` — closes existing mirror windows and opens one per region.
    - `Task<RegionModel?> AddRegionAsync()` — opens `RegionSelectorWindow`; returns null if cancelled or Tibia not found.
    - `Task RecropAsync(Guid id)`, `void Remove(Guid id)`, `void Rename(Guid id, string name)`, `void SetVisible(Guid id, bool v)`, `void SetLocked(Guid id, bool v)`, `void SetOpacity(Guid id, double o)`
    - `bool AllHidden { get; set; }` (toggles all mirrors without changing per-region `IsVisible`), `void LockAll()`.
    - `event Action<Guid, PixelRect, bool>? MirrorLayoutChanged` — (region id, mirror bounds in screen physical px, currently shown). Raised on move/resize/show/hide/Tibia state change. Used by Task 11.
    - `void Flash(Guid id, int pulses, string colorHex)` — pulses the mirror border (250 ms on / 250 ms off).
    - `IReadOnlyList<RegionModel> Regions` (live list from the loaded profile).
  - `MirrorsViewModel` (CommunityToolkit `ObservableObject`): `ObservableCollection<MirrorCardViewModel> Cards`, `AddRegionCommand`; `MirrorCardViewModel` exposes `Name`, `IsVisible`, `IsLocked`, `Opacity`, `RenameCommand`, `RecropCommand`, `DeleteCommand`, plus `CountdownSettings Countdown` and a `public object? CountdownEditor { get; set; }` placeholder slot that Task 11 fills (a `ContentPresenter` in the card's expander).

- [ ] **Step 1: Write the failing tests** (pure visibility rule)

```csharp
using TibiaMirror.Abstractions; using TibiaMirror.Detection; using TibiaMirror.Mirrors; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Mirrors;

public class MirrorVisibilityTests
{
    static readonly WindowInfo W = new(1, "Tibia", "client", true, false, new(0, 0, 800, 600), new(8, 31, 784, 561));
    static TibiaState Found(bool focused = true, bool min = false) => new(W with { IsMinimized = min }, focused);
    static readonly RegionModel R = new() { IsVisible = true };

    [Fact] public void Shown_when_tibia_found() => Assert.True(MirrorVisibility.ShouldShow(R, Found(), false, false));
    [Fact] public void Hidden_when_tibia_missing() => Assert.False(MirrorVisibility.ShouldShow(R, new TibiaState(null, false), false, false));
    [Fact] public void Hidden_when_tibia_minimized() => Assert.False(MirrorVisibility.ShouldShow(R, Found(min: true), false, false));
    [Fact] public void Hidden_when_all_hidden() => Assert.False(MirrorVisibility.ShouldShow(R, Found(), true, false));
    [Fact] public void Hidden_when_region_hidden() => Assert.False(MirrorVisibility.ShouldShow(new RegionModel { IsVisible = false }, Found(), false, false));
    [Fact] public void Focus_rule_applies_only_when_enabled()
    {
        Assert.True(MirrorVisibility.ShouldShow(R, Found(focused: false), false, false));
        Assert.False(MirrorVisibility.ShouldShow(R, Found(focused: false), false, true));
    }
}
```

- [ ] **Step 2: Run, verify fail.**
- [ ] **Step 3: Implement `MirrorVisibility`; run tests, verify pass.**

- [ ] **Step 4: Implement the windows and service**

`RegionSelectorWindow`: borderless, `AllowsTransparency=True`, `Background="#01000000"`, `Topmost`, `ShowInTaskbar=False`; on `SourceInitialized` call `WindowStyles.SetBoundsPhysical(hwnd, tibia.ClientBounds)`. Overlay `Canvas` with a 40%-black `Path` using `CombinedGeometry Exclude` so the dragged rectangle is clear, a 2 px accent (`#FFFF7F00`) outline, and a size label "W × H". Mouse down/move/up track points; convert DIP → physical with `DpiMath.ToPhysical(p, DpiMath.Scale(Monitors.DpiForWindow(hwnd)))` and add `ClientBounds.X/Y`. Esc or right-click → cancel. On mouse up: `CropMath.FromSelection(selection, client)`; empty → keep window open and flash the label "Too small"; else complete. Exposed as `static Task<PixelRect?> SelectAsync(TibiaState tibia)` which returns the client-relative crop.

`MirrorWindow`: borderless (`WindowStyle=None`, `ResizeMode=NoResize`, `AllowsTransparency=False`, `Background=Black`), `Topmost`, `ShowInTaskbar=False`. On `SourceInitialized`: `WindowStyles.MakeToolWindow(hwnd)`, apply bounds physically, create `DwmThumbnail(hwnd, tibiaHwnd)`. The thumbnail destination is the client area inset by 2 px on each side; a WPF `Border` (2 px) around the edge is accent-colored when unlocked, transparent when locked, and used by `Flash`. Unlocked: left-drag anywhere = move (`DragMove`), 12×12 px corner handles = resize via `CropMath.ResizeKeepingAspect` with aspect = crop W/H (hold Shift = free resize, min 8×8). After each move/resize: read `WindowStyles.GetBoundsPhysical`, store in `RegionModel.Mirror`, call `onChanged`, raise layout event. Locked: `WindowStyles.SetClickThrough(hwnd, true)`. On Tibia state change: if handle changed → dispose and recreate the thumbnail; recompute `rcSource = CropMath.ToThumbnailSource(region.Crop, tibia.WindowBounds, tibia.ClientBounds)`; `Update(...)`. Show/hide via `Show()`/`Hide()` according to `MirrorVisibility.ShouldShow`. If `DwmThumbnail` construction throws: log, show a centered `TextBlock` "Can't mirror — is Tibia minimized?" until the next successful attach.

`MirrorService.AddRegionAsync`: requires `locator.State.IsFound && !IsMinimized`; else `MessageBox.Show("Start Tibia first, then click Add Region.")` and return null. After selection: name `"Region " + (count + 1)`; mirror bounds = crop size at 1:1, placed to the right of the crop on screen (`client.X + crop.Right + 20`, `client.Y + crop.Y`), then `MonitorClamp.Clamp(rect, Monitors.WorkAreas(), Monitors.PrimaryWorkArea())`; append to `profile.Regions`; open window; `onChanged()`.

`MirrorsView.xaml`: top row **＋ Add Region** button; `ItemsControl` of cards. Card: `TextBlock` name (double-click or ✏ "Rename" opens `RenameDialog` — `TextBox` 1–40 chars, OK/Cancel), toggle buttons 👁 (`IsVisible`) and 🔒 (`IsLocked`), opacity `Slider` 10–100%, buttons "Re-crop", "Delete" (confirm "Delete '<name>'?"), and an `Expander Header="Countdown"` containing `ContentPresenter Content="{Binding CountdownEditor}"`. Empty state text: "No mirrors yet. Start Tibia and click Add Region."

MainWindow buttons: **Show/Hide all** toggles `MirrorService.AllHidden`; **Lock all** calls `LockAll()`. Tray "Show/Hide mirrors" → `host.RequestToggleMirrors()` → same toggle. App-wide hotkey `Settings.ShowHideAllHotkey` registered with `HotkeyScope.Global` (re-registered when settings change — Task 10 calls `AppHost.ApplySettings()`, which this task creates with the show/hide registration inside).

AppHost: construct `MirrorService` in `Start()`, `Mirrors.Load(CurrentProfile)`, subscribe `ToggleMirrorsRequested`.

- [ ] **Step 5: Run full tests; manual check** with Tibia running: add 2 regions; drag/resize mirrors; move the Tibia window → mirrors keep showing the same area; minimize Tibia → mirrors hide, restore → back; lock → click-through; rename/delete; Show/Hide all; 150% DPI if available.
- [ ] **Step 6: Commit** — `git commit -m "feat: mirror regions with DWM thumbnails, selector and Mirrors tab"`

---

### Task 8: Profiles (service + Profiles tab + toast)

**Files:**
- Create: `src/TibiaMirror/Profiles/ProfileService.cs`, `ProfileNameRules.cs`, `ProfilesViewModel.cs`, `ProfileNameDialog.xaml(.cs)`, `Toast.xaml(.cs)`
- Modify: `src/TibiaMirror/Views/ProfilesView.xaml(.cs)`, `src/TibiaMirror/AppHost.cs`
- Test: `tests/TibiaMirror.Tests/Profiles/ProfileServiceTests.cs`, `ProfileNameRulesTests.cs`

**Interfaces:**
- Consumes: `Profile` (T1), `AppPaths` (T1), `JsonStore`, `LoadStatus` (T3).
- Produces:
  - `static class ProfileNameRules { static string? Validate(string name, IEnumerable<string> existing, string? renamingFrom = null) }` — returns an error message or null. Rules: trimmed length 1–40; no `Path.GetInvalidFileNameChars()`; not a reserved device name (`CON, PRN, AUX, NUL, COM1–COM9, LPT1–LPT9`, case-insensitive, also with an extension); no trailing `.`; unique case-insensitively (except equal to `renamingFrom`).
  - `sealed class ProfileService(AppPaths paths)`
    - `Profile Current { get; }`, `event Action<Profile>? CurrentChanged;`, `event Action<string>? Notice;` (user-facing messages, e.g. corruption recovered)
    - `void LoadInitial()` — reads `last_profile`; loads it, else first existing profile alphabetically, else creates and saves "Default".
    - `IReadOnlyList<string> List()` — names from `profiles\*.json`, sorted case-insensitively.
    - `void SaveCurrent()`, `Profile Create(string name)`, `Profile Duplicate(string source, string newName)`, `void Rename(string oldName, string newName)`, `void Delete(string name)`, `void Switch(string name)`, `void SwitchNext()`, `string Import(string jsonPath)` (returns the final name; appends " (2)", " (3)"… on collision), `void Export(string name, string destinationPath)`.
    - All mutating methods validate with `ProfileNameRules` and throw `ArgumentException(message)` on invalid input; `Delete` of the last profile throws `InvalidOperationException("You need at least one profile.")`. `Switch`/`Create`/`Duplicate` save the current profile first and write `last_profile`.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Models; using TibiaMirror.Profiles;
namespace TibiaMirror.Tests.Profiles;

public class ProfileNameRulesTests                                     // Review Focus #5
{
    static readonly string[] Existing = { "Default", "Knight" };
    [Theory]
    [InlineData("", "empty")] [InlineData("   ", "empty")]
    [InlineData("Knight/PvP", "character")] [InlineData("a:b", "character")]
    [InlineData("CON", "reserved")] [InlineData("com1", "reserved")] [InlineData("nul.txt", "reserved")]
    [InlineData("default", "exists")] [InlineData("ends.", "end")]
    public void Rejects(string name, string fragment) =>
        Assert.Contains(fragment, ProfileNameRules.Validate(name, Existing)!, StringComparison.OrdinalIgnoreCase);
    [Fact] public void Rejects_over_40_chars() => Assert.NotNull(ProfileNameRules.Validate(new string('a', 41), Existing));
    [Fact] public void Accepts_normal_name() => Assert.Null(ProfileNameRules.Validate("Paladin Hunt", Existing));
    [Fact] public void Rename_to_same_name_different_case_is_ok() => Assert.Null(ProfileNameRules.Validate("knight", Existing, renamingFrom: "Knight"));
}
```
Error messages must contain those fragments: "Name can't be empty", "Name can't contain the character 'X'", "'CON' is a reserved Windows name", "A profile named 'X' already exists", "Name can't end with a dot", "Name must be 40 characters or fewer".

```csharp
public class ProfileServiceTests : IDisposable
{
    readonly string _root = Directory.CreateTempSubdirectory("tm-prof").FullName;
    public void Dispose() => Directory.Delete(_root, true);
    ProfileService Make() { var s = new ProfileService(new AppPaths(_root)); s.LoadInitial(); return s; }

    [Fact] public void First_run_creates_Default() { var s = Make(); Assert.Equal("Default", s.Current.Name); Assert.Equal(new[] { "Default" }, s.List()); }

    [Fact] public void Create_switches_and_remembers_last()
    {
        var s = Make(); s.Create("Knight");
        Assert.Equal("Knight", s.Current.Name);
        Assert.Equal("Knight", Make().Current.Name);   // fresh service reads last_profile
    }

    [Fact] public void Duplicate_copies_regions_with_new_ids()
    {
        var s = Make(); s.Current.Regions.Add(new RegionModel { Name = "HP" }); s.SaveCurrent();
        s.Duplicate("Default", "Copy");
        Assert.Equal("Copy", s.Current.Name); Assert.Equal("HP", s.Current.Regions[0].Name);
        s.Switch("Default"); var origId = s.Current.Regions[0].Id; s.Switch("Copy");
        Assert.NotEqual(origId, s.Current.Regions[0].Id);
    }

    [Fact] public void Rename_moves_file_and_updates_current()
    {
        var s = Make(); s.Rename("Default", "Main");
        Assert.Equal(new[] { "Main" }, s.List()); Assert.Equal("Main", s.Current.Name);
    }

    [Fact] public void Cannot_delete_last_profile() { var s = Make(); Assert.Throws<InvalidOperationException>(() => s.Delete("Default")); }

    [Fact] public void Delete_current_switches_to_another()
    {
        var s = Make(); s.Create("Knight"); s.Delete("Knight");
        Assert.Equal("Default", s.Current.Name); Assert.Equal(new[] { "Default" }, s.List());
    }

    [Fact] public void SwitchNext_cycles_alphabetically()
    {
        var s = Make(); s.Create("B"); s.Create("A"); s.Switch("A");
        s.SwitchNext(); Assert.Equal("B", s.Current.Name);
        s.SwitchNext(); Assert.Equal("Default", s.Current.Name);
        s.SwitchNext(); Assert.Equal("A", s.Current.Name);
    }

    [Fact] public void Invalid_name_throws_and_creates_no_file()
    {
        var s = Make(); Assert.Throws<ArgumentException>(() => s.Create("Knight/PvP"));
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "profiles"), "*.json"));
    }

    [Fact] public void Import_renames_on_collision()
    {
        var s = Make(); var exported = Path.Combine(_root, "exp.json"); s.Export("Default", exported);
        Assert.Equal("Default (2)", s.Import(exported));
    }

    [Fact] public void Corrupt_last_profile_raises_notice()
    {
        var s = Make(); File.WriteAllText(Path.Combine(_root, "profiles", "Default.json"), "{bad");
        var s2 = new ProfileService(new AppPaths(_root)); string? msg = null; s2.Notice += m => msg = m; s2.LoadInitial();
        Assert.NotNull(msg);
    }
}
```

- [ ] **Step 2: Run, verify fail.**
- [ ] **Step 3: Implement** `ProfileNameRules` and `ProfileService` using `JsonStore.Load/Save` on `paths.ProfileFile(name)`. Profile `Name` inside the file is overwritten by the file name on load. `Import` takes the name from the JSON's `Name` (fallback: the file name without extension), validates it, and appends " (2)", " (3)"… until unique. `Duplicate` deep-copies via JSON round-trip and assigns `Guid.NewGuid()` to every region and timer. `Notice` messages: `RestoredFromBackup` → "Profile '<n>' was damaged and has been restored from its backup."; `Corrupt` → "Profile '<n>' was damaged. A fresh copy was started; the damaged file was kept as <file>."
- [ ] **Step 4: Run, verify pass.**

- [ ] **Step 5: UI.** `ProfilesView`: `ListBox` of names (current shown bold with "✓"), buttons **New**, **Duplicate**, **Rename**, **Delete**, **Switch** (also double-click), **Import…**, **Export…** (`OpenFileDialog`/`SaveFileDialog`, filter `TibiaMirror profile (*.json)|*.json`). `ProfileNameDialog`: `TextBox` + live validation text from `ProfileNameRules.Validate`, OK disabled while invalid. Errors from the service are shown with `MessageBox`. `Toast`: small borderless topmost non-activating window at top-center of the primary work area, text "Profile: <name>", fades out after 1.5 s.

AppHost: `Profiles = new ProfileService(Paths); Profiles.LoadInitial(); CurrentProfile = Profiles.Current;` subscribe `CurrentChanged` → `CurrentProfile = p; Mirrors.Load(p); ProfileChanged?.Invoke(p)` (add `public event Action<Profile>? ProfileChanged;`) and show the toast; `Notice` → `MessageBox`. MainWindow `ProfileText` shows `"Profile: " + name`. `AppHost.ApplySettings()` additionally registers `Settings.NextProfileHotkey` (Global) → `Profiles.SwitchNext()`.

- [ ] **Step 6: Run full tests; manual check:** create/duplicate/rename/delete/switch; mirrors swap on switch; toast appears; import/export round-trip.
- [ ] **Step 7: Commit** — `git commit -m "feat: profiles with validation, import/export and switch toast"`

---

### Task 9: Audio timers (service + Audio Timers tab)

**Files:**
- Create: `src/TibiaMirror/Timers/AudioTimerService.cs`, `AudioTimersViewModel.cs`, `AudioTimerCardViewModel.cs`, `TimerEditDialog.xaml(.cs)`
- Modify: `src/TibiaMirror/Views/AudioTimersView.xaml(.cs)`, `src/TibiaMirror/AppHost.cs`
- Test: `tests/TibiaMirror.Tests/Timers/AudioTimerServiceTests.cs`

**Interfaces:**
- Consumes: `AudioTimerModel`, `RepressBehavior`, `Profile`, `IClock`, `ISoundPlayer` (T1); `BuiltInSounds` (T6); `HotkeyService`, `HotkeyScope`, `HotkeyBox` (T4).
- Produces:
  - `sealed class AudioTimerService(IClock clock, ISoundPlayer sounds)`
    - `void Load(Profile profile)` — clears running timers.
    - `void Trigger(Guid timerId)` — no-op unless `profile.IsListening`; applies `OnRepress`.
    - `void Cancel(Guid timerId)`, `bool IsRunning(Guid id)`, `TimeSpan? Remaining(Guid id)`
    - `void Tick()` — completes due timers: plays `timer.Sound` at `profile.MasterVolume/100.0 * timer.Volume/100.0`, raises `event Action<Guid>? Completed`.
    - `void TestSound(Guid id)` — plays immediately at effective volume.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Abstractions; using TibiaMirror.Models; using TibiaMirror.Timers;
namespace TibiaMirror.Tests.Timers;

sealed class FakeClock : IClock { public DateTime UtcNow { get; set; } = new(2026, 1, 1); public void Advance(double s) => UtcNow = UtcNow.AddSeconds(s); }
sealed class FakeSounds : ISoundPlayer
{
    public List<(string Id, double Vol)> Played = new();
    public void Play(string id, double v) => Played.Add((id, v));
    public bool CanPlay(string id) => true;
}

public class AudioTimerServiceTests
{
    readonly FakeClock _clock = new(); readonly FakeSounds _snd = new();
    readonly AudioTimerModel _t = new() { DurationSeconds = 10, Sound = "builtin:ding", Volume = 50 };
    AudioTimerService Make(bool listening = true, int master = 80)
    {
        var p = new Profile { IsListening = listening, MasterVolume = master }; p.Timers.Add(_t);
        var s = new AudioTimerService(_clock, _snd); s.Load(p); return s;
    }

    [Fact] public void Plays_once_when_duration_elapses()
    {
        var s = Make(); s.Trigger(_t.Id);
        _clock.Advance(9.9); s.Tick(); Assert.Empty(_snd.Played);
        _clock.Advance(0.2); s.Tick(); s.Tick();
        Assert.Single(_snd.Played); Assert.Equal("builtin:ding", _snd.Played[0].Id); Assert.False(s.IsRunning(_t.Id));
    }

    [Fact] public void Effective_volume_is_master_times_timer() { var s = Make(master: 80); s.Trigger(_t.Id); _clock.Advance(11); s.Tick(); Assert.Equal(0.4, _snd.Played[0].Vol, 3); }

    [Fact] public void Not_listening_ignores_trigger() { var s = Make(listening: false); s.Trigger(_t.Id); Assert.False(s.IsRunning(_t.Id)); }

    [Fact] public void Restart_policy_resets_remaining()
    {
        var s = Make(); s.Trigger(_t.Id); _clock.Advance(6); s.Trigger(_t.Id);
        Assert.Equal(10, s.Remaining(_t.Id)!.Value.TotalSeconds, 2);
    }

    [Fact] public void Ignore_policy_keeps_remaining()
    {
        _t.OnRepress = RepressBehavior.IgnoreWhileRunning;
        var s = Make(); s.Trigger(_t.Id); _clock.Advance(6); s.Trigger(_t.Id);
        Assert.Equal(4, s.Remaining(_t.Id)!.Value.TotalSeconds, 2);
    }

    [Fact] public void Cancel_stops_without_sound() { var s = Make(); s.Trigger(_t.Id); s.Cancel(_t.Id); _clock.Advance(20); s.Tick(); Assert.Empty(_snd.Played); }

    [Fact] public void Completed_event_raised() { var s = Make(); Guid? done = null; s.Completed += id => done = id; s.Trigger(_t.Id); _clock.Advance(10); s.Tick(); Assert.Equal(_t.Id, done); }

    [Fact] public void Unknown_timer_id_is_ignored() { var s = Make(); s.Trigger(Guid.NewGuid()); s.Tick(); Assert.Empty(_snd.Played); }

    [Fact] public void Load_clears_running() { var s = Make(); s.Trigger(_t.Id); s.Load(new Profile { IsListening = true }); Assert.False(s.IsRunning(_t.Id)); }
}
```

- [ ] **Step 2: Run, verify fail.**
- [ ] **Step 3: Implement** with a `Dictionary<Guid, DateTime> _endsAt`. `Remaining = max(0, endsAt - now)`.
- [ ] **Step 4: Run, verify pass.**

- [ ] **Step 5: UI.** `AudioTimersView`: header row: **Listening** toggle (big ON/OFF, bound to `Profile.IsListening`), "Start listening when the app opens" checkbox, master volume slider 0–100, **＋ Add Timer**. Card list: name, hotkey text, duration, live remaining "4.2 s" (or "Ready"), buttons **Test**, **Edit**, **Delete** (confirm); warning icon "⚠ Sound file not found — using Chime" when `!sounds.CanPlay(timer.Sound)`. `TimerEditDialog`: Name (1–40 chars), `HotkeyBox`, duration `TextBox` (parse with `CultureInfo.CurrentCulture` then invariant; 0.1–3600, message "Enter a duration between 0.1 and 3600 seconds"), sound `ComboBox` of `BuiltInSounds.All` + "Custom file…" (OpenFileDialog `Audio (*.mp3;*.wav)|*.mp3;*.wav`), volume slider, re-press radio (Restart / Ignore while running), **Test** button. On save, check `HotkeyService.FindConflicts` and, if any, ask "This key is already used by: <names>. Use it anyway?". Empty state: "No timers yet. Click Add Timer, pick a key and a duration."

AppHost: `Timers = new AudioTimerService(new SystemClock(), Sounds)`; on profile load: `Timers.Load(p)`, if `p.StartListeningOnOpen` (only at app start) set `p.IsListening = true`; register each timer's hotkey (`HotkeyScope.WhenTibiaFocused`, owner = timer model) → `Timers.Trigger(id)`; dispose registrations on reload/edit. A 100 ms `DispatcherTimer` calls `Timers.Tick()` and refreshes card remaining text. Edits call the AppHost `onChanged` autosave hook (Task 10; until then a no-op `Action ProfileEdited` on AppHost).

- [ ] **Step 6: Manual check:** add a 5 s timer on F2 → press F2 in Tibia → sound after 5 s; press F2 in Notepad → nothing; Listening OFF → nothing; custom mp3 works; deleted mp3 shows warning and plays Chime.
- [ ] **Step 7: Commit** — `git commit -m "feat: audio timers with hotkeys, volumes and re-press policy"`

---

### Task 10: Persistence wiring, settings window, restore

**Files:**
- Create: `src/TibiaMirror/Settings/SettingsService.cs`, `SettingsWindow.xaml(.cs)`
- Modify: `src/TibiaMirror/AppHost.cs`, `src/TibiaMirror/App.xaml.cs`, `src/TibiaMirror/Views/MainWindow.xaml(.cs)`
- Test: `tests/TibiaMirror.Tests/Settings/SettingsServiceTests.cs`, `tests/TibiaMirror.Tests/Settings/RestoreClampTests.cs`

**Interfaces:**
- Consumes: everything above; especially `JsonStore`, `Debouncer` (T3), `ProfileService` (T8), `MirrorService` (T7), `MonitorClamp` (T2), `Monitors` (T5).
- Produces:
  - `sealed class SettingsService(AppPaths paths)` — `AppSettings Current`, `LoadStatus LastLoadStatus`, `void Load()`, `void Save()`.
  - `static class RestoreClamp { static void ClampProfile(Profile p, IReadOnlyList<PixelRect> workAreas, PixelRect primary) }` — clamps every `RegionModel.Mirror`.
  - `AppHost.ProfileEdited()` — triggers a 1 s `Debouncer` that saves profile + settings (marshalled to the UI thread with `Dispatcher.Invoke` for a consistent snapshot). `AppHost.Dispose()` flushes it.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Models; using TibiaMirror.Settings; using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Settings;

public class SettingsServiceTests : IDisposable
{
    readonly string _root = Directory.CreateTempSubdirectory("tm-set").FullName;
    public void Dispose() => Directory.Delete(_root, true);

    [Fact] public void Defaults_on_first_run()
    {
        var s = new SettingsService(new AppPaths(_root)); s.Load();
        Assert.Equal("client", s.Current.TibiaProcessName); Assert.Equal(LoadStatus.Missing, s.LastLoadStatus);
    }
    [Fact] public void Roundtrip_hotkeys_and_flags()
    {
        var s = new SettingsService(new AppPaths(_root)); s.Load();
        s.Current.ShowHideAllHotkey = new Hotkey(0x7B, HotkeyModifiers.Ctrl); s.Current.OnlyShowWhenTibiaFocused = true; s.Save();
        var s2 = new SettingsService(new AppPaths(_root)); s2.Load();
        Assert.Equal(new Hotkey(0x7B, HotkeyModifiers.Ctrl), s2.Current.ShowHideAllHotkey); Assert.True(s2.Current.OnlyShowWhenTibiaFocused);
    }
}

public class RestoreClampTests
{
    [Fact] public void Mirrors_on_missing_monitor_are_moved_to_primary()      // Review Focus #2
    {
        var p = new Profile(); p.Regions.Add(new RegionModel { Mirror = new PixelRect(3000, 200, 300, 200) });
        RestoreClamp.ClampProfile(p, new[] { new PixelRect(0, 0, 1920, 1040) }, new PixelRect(0, 0, 1920, 1040));
        var m = p.Regions[0].Mirror; Assert.True(m.X >= 0 && m.Right <= 1920 && m.Y >= 0 && m.Bottom <= 1040);
    }
}
```

- [ ] **Step 2: Run, verify fail.** **Step 3: Implement.** **Step 4: Run, verify pass.**

- [ ] **Step 5: Wire it up**
  - App start order in `AppHost.Start()`: `SettingsService.Load()` → `Profiles.LoadInitial()` → `RestoreClamp.ClampProfile(CurrentProfile, Monitors.WorkAreas(), Monitors.PrimaryWorkArea())` → start locator → `Mirrors.Load` → timers → `ApplySettings()`.
  - Every user edit path (MirrorService `onChanged`, timer/countdown edits, profile toggles, settings changes, mirror moves) calls `ProfileEdited()`.
  - `App` exit and `SessionEnding` → `host.Dispose()` (flushes debouncer).
  - Main window bounds saved in `Settings.MainWindowBounds` on close/move and restored (clamped) at start.
  - `SettingsWindow` (⚙ button): Show/Hide all hotkey (`HotkeyBox`), Next profile hotkey, "Only show mirrors while Tibia is focused", "Advanced" expander: Tibia process name and title prefix (non-empty), **Open data folder** button (`explorer.exe <Root>`), version text `AppInfo.Version`. Save → `SettingsService.Save()` + `ApplySettings()`.
  - If `HotkeysUnavailable` (Task 4) → yellow banner in MainWindow: "Hotkeys are unavailable. [Retry]" — Retry recreates the hook.
  - `SettingsService.LastLoadStatus` of `RestoredFromBackup`/`Corrupt` → one-time `MessageBox` notice like profiles.

- [ ] **Step 6: Manual check:** make changes → wait 2 s → kill the process in Task Manager → relaunch → state restored. Move a mirror to a second monitor, exit, unplug/disable the monitor, launch → mirror is on the primary monitor.
- [ ] **Step 7: Commit** — `git commit -m "feat: autosave, restore with monitor clamping and settings window"`

---

### Task 11: Countdown bar

**Files:**
- Create: `src/TibiaMirror/Countdown/CountdownService.cs`, `CountdownBarWindow.xaml(.cs)`, `CountdownBarLayout.cs`, `CountdownEditor.xaml(.cs)`
- Modify: `src/TibiaMirror/AppHost.cs`, `src/TibiaMirror/Mirrors/MirrorsViewModel.cs` (fill `CountdownEditor`)
- Test: `tests/TibiaMirror.Tests/Countdown/CountdownServiceTests.cs`, `CountdownBarLayoutTests.cs`

**Interfaces:**
- Consumes: `CountdownSettings`, `BarSide`, `BarShape`, `RegionModel`, `IClock`, `ISoundPlayer` (T1); `MirrorService.MirrorLayoutChanged`, `MirrorService.Flash` (T7); `HotkeyService` (T4); `WindowStyles` (T5).
- Produces:
  - `sealed class CountdownService(IClock clock)` — `void Start(Guid regionId, double durationSeconds)` (restarts if running), `void Stop(Guid id)`, `double Progress(Guid id)` (1.0 just started → 0.0 done; 0 when not running), `bool IsRunning(Guid id)`, `void Tick()` raising `event Action<Guid>? Completed` once per run.
  - `static class CountdownBarLayout { static PixelRect Compute(PixelRect mirror, BarSide side, BarShape shape, int thickness) }` — FullEdge: bar spans the full mirror edge, outside the mirror, touching it. NarrowStrip: same but length = 25% of the edge (min 24 px), centered.
  - `CountdownBarWindow`: click-through, tool window, topmost; draws a filled `Rectangle` whose length is `Progress × full length`, draining toward the start of the edge (top/left), `Color` from settings.

- [ ] **Step 1: Write the failing tests**

```csharp
using TibiaMirror.Countdown; using TibiaMirror.Models; using TibiaMirror.Tests.Timers;
namespace TibiaMirror.Tests.Countdown;

public class CountdownServiceTests
{
    readonly FakeClock _clock = new(); readonly Guid _id = Guid.NewGuid();
    [Fact] public void Progress_drains_linearly()
    {
        var s = new CountdownService(_clock); s.Start(_id, 10);
        Assert.Equal(1.0, s.Progress(_id), 3); _clock.Advance(2.5); Assert.Equal(0.75, s.Progress(_id), 3);
    }
    [Fact] public void Completes_once()
    {
        var s = new CountdownService(_clock); int n = 0; s.Completed += _ => n++;
        s.Start(_id, 1); _clock.Advance(1.01); s.Tick(); s.Tick();
        Assert.Equal(1, n); Assert.False(s.IsRunning(_id)); Assert.Equal(0, s.Progress(_id));
    }
    [Fact] public void Restart_refills()
    {
        var s = new CountdownService(_clock); s.Start(_id, 10); _clock.Advance(8); s.Start(_id, 10);
        Assert.Equal(1.0, s.Progress(_id), 3);
    }
    [Fact] public void Stop_prevents_completion() { var s = new CountdownService(_clock); int n = 0; s.Completed += _ => n++; s.Start(_id, 1); s.Stop(_id); _clock.Advance(5); s.Tick(); Assert.Equal(0, n); }
}

public class CountdownBarLayoutTests
{
    static readonly PixelRect M = new(100, 100, 200, 100);
    [Fact] public void Bottom_full() => Assert.Equal(new PixelRect(100, 200, 200, 6), CountdownBarLayout.Compute(M, BarSide.Bottom, BarShape.FullEdge, 6));
    [Fact] public void Top_full() => Assert.Equal(new PixelRect(100, 94, 200, 6), CountdownBarLayout.Compute(M, BarSide.Top, BarShape.FullEdge, 6));
    [Fact] public void Left_full() => Assert.Equal(new PixelRect(94, 100, 6, 100), CountdownBarLayout.Compute(M, BarSide.Left, BarShape.FullEdge, 6));
    [Fact] public void Right_narrow_is_centered_quarter() => Assert.Equal(new PixelRect(300, 137, 6, 25), CountdownBarLayout.Compute(M, BarSide.Right, BarShape.NarrowStrip, 6));
    [Fact] public void Narrow_has_minimum_length() => Assert.Equal(24, CountdownBarLayout.Compute(new PixelRect(0, 0, 40, 40), BarSide.Bottom, BarShape.NarrowStrip, 4).Width);
}
```
(Narrow length = `max(24, edge * 25 / 100)` using integer math; offset = `(edge - length) / 2` integer division. Right narrow: 25 px, y = 100 + 37 = 137.)

- [ ] **Step 2: Run, verify fail.** **Step 3: Implement.** **Step 4: Run, verify pass.**

- [ ] **Step 5: Wire it up.**
  - AppHost: `Countdowns = new CountdownService(new SystemClock())`. For every region with `Countdown.Enabled && Countdown.Hotkey != null`, register `HotkeyScope.WhenTibiaFocused` → `Countdowns.Start(region.Id, Clamp(DurationSeconds, 0.5, 3600))`; re-register on profile load and on edits.
  - One `CountdownBarWindow` per enabled region, created lazily; positioned with `WindowStyles.SetBoundsPhysical(CountdownBarLayout.Compute(mirror, side, shape, thickness))` on `MirrorLayoutChanged`; hidden when the mirror is hidden or the countdown isn't running.
  - A 16 ms `DispatcherTimer` (only while any countdown is running) calls `Tick()` and updates bar fill.
  - `Completed` → if `FlashOnDone` → `Mirrors.Flash(id, 5, settings.Color)`; if `Sound` set → `Sounds.Play(sound, profile.MasterVolume / 100.0)`; hide bar.
  - `CountdownEditor` (in each mirror card's "Countdown" expander): Enabled checkbox, `HotkeyBox`, duration (0.5–3600), side `ComboBox`, shape radio (Full edge / Narrow strip), color swatches (orange `#FFFF7F00`, red `#FFE53935`, green `#FF43A047`, blue `#FF1E88E5`, white `#FFFFFFFF`, yellow `#FFFDD835`) plus hex `TextBox`, thickness slider 2–40, "Flash border when done" checkbox, optional sound `ComboBox` ("None" + built-ins + custom). Conflict check as in Task 9. Every change → `ProfileEdited()`.

- [ ] **Step 6: Manual check:** each side and both shapes; bar follows the mirror while dragging; re-press restarts; flash happens 5×; hidden mirror hides bar; Tibia unfocused → hotkey ignored.
- [ ] **Step 7: Commit** — `git commit -m "feat: per-mirror countdown bar with flash and sound"`

---

### Task 12: Installer, release workflow, README and docs

**Files:**
- Create: `installer/TibiaMirror.iss`, `.github/workflows/release.yml`, `README.md`, `CHANGELOG.md`, `docs/manual-test-checklist.md`, `src/TibiaMirror/Assets/Icons/app.ico`, `docs/images/.gitkeep`
- Modify: `src/TibiaMirror/TibiaMirror.csproj` (`<ApplicationIcon>`), `src/TibiaMirror/Shell/TrayIcon.cs` (use the app icon)

**Interfaces:**
- Consumes: the built app.
- Produces: release artifacts `TibiaMirror-Setup-X.Y.Z.exe`, `TibiaMirror-Portable-X.Y.Z.zip`.

- [ ] **Step 1: App icon.** Generate `app.ico` (16/32/48/256 px) with a small PowerShell + `System.Drawing` script kept at `tools/make-icon.ps1`: dark rounded square `#FF1E1E1E` with an orange `#FFFF7F00` "mirror" glyph (two overlapping rectangles). Reference it as `<ApplicationIcon>Assets\Icons\app.ico</ApplicationIcon>` and as an embedded `Resource`; tray uses it.

- [ ] **Step 2: Inno Setup script** `installer/TibiaMirror.iss`:
```ini
#define AppVersion GetEnv("TM_VERSION")
#if AppVersion == ""
  #define AppVersion "0.0.0"
#endif
[Setup]
AppId={{6C3E9B2A-6E0B-4C59-9E0E-7A1B2C3D4E5F}
AppName=TibiaMirror
AppVersion={#AppVersion}
AppPublisher=TibiaMirror contributors
DefaultDirName={autopf}\TibiaMirror
DefaultGroupName=TibiaMirror
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts
OutputBaseFilename=TibiaMirror-Setup-{#AppVersion}
SetupIconFile=..\src\TibiaMirror\Assets\Icons\app.ico
UninstallDisplayIcon={app}\TibiaMirror.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Start TibiaMirror when Windows starts"; Flags: unchecked
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
[Icons]
Name: "{group}\TibiaMirror"; Filename: "{app}\TibiaMirror.exe"
Name: "{autodesktop}\TibiaMirror"; Filename: "{app}\TibiaMirror.exe"; Tasks: desktopicon
Name: "{userstartup}\TibiaMirror"; Filename: "{app}\TibiaMirror.exe"; Tasks: startup
[Run]
Filename: "{app}\TibiaMirror.exe"; Description: "Launch TibiaMirror"; Flags: nowait postinstall skipifsilent
```
User data in `%APPDATA%\TibiaMirror` is intentionally kept on uninstall (README says how to remove it).

- [ ] **Step 3: Release workflow** `.github/workflows/release.yml`:
```yaml
name: release
on:
  push:
    tags: ['v*']
permissions:
  contents: write
jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - name: Version from tag
        shell: pwsh
        run: echo "TM_VERSION=$('${{ github.ref_name }}'.TrimStart('v'))" >> $env:GITHUB_ENV
      - run: dotnet test tests/TibiaMirror.Tests -c Release
      - name: Publish
        run: dotnet publish src/TibiaMirror/TibiaMirror.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=${{ env.TM_VERSION }} -o publish
      - name: Portable zip
        shell: pwsh
        run: |
          New-Item -ItemType Directory -Force artifacts | Out-Null
          Compress-Archive -Path publish\* -DestinationPath "artifacts\TibiaMirror-Portable-$env:TM_VERSION.zip"
      - name: Installer
        shell: pwsh
        run: |
          if (-not (Test-Path "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe")) { choco install innosetup -y --no-progress }
          & "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer\TibiaMirror.iss
      - uses: softprops/action-gh-release@v2
        with:
          files: artifacts/*
          generate_release_notes: true
```
Also add `publish/` and `artifacts/` to `.gitignore`.

- [ ] **Step 4: Local verification of packaging.** `dotnet publish ... -o publish` then `winget install --id JRSoftware.InnoSetup -e` (if not installed) and `ISCC installer\TibiaMirror.iss` with `$env:TM_VERSION="0.1.0"` → `artifacts\TibiaMirror-Setup-0.1.0.exe` exists; install it, launch from Start menu, uninstall.

- [ ] **Step 5: README.md** — sections in this order, plain language (aimed at non-technical players), short sentences:
  1. Title + one-line pitch: "Mirror any part of your Tibia screen anywhere you want — health bars, cooldowns, the minimap — plus audio timers and countdown bars."
  2. Screenshot `docs/images/overview.png` (placeholder path; the orchestrator adds real screenshots).
  3. **⬇ Install (3 steps)**: (1) Go to the **[Releases page](../../releases/latest)** and download `TibiaMirror-Setup-<version>.exe`. (2) Run it. If Windows shows "Windows protected your PC", click **More info → Run anyway** (the app is free and not code-signed; explain in one sentence why). (3) Open TibiaMirror from the Start menu, then start Tibia — the dot at the top turns green.
  4. **No-install option**: download the Portable zip, right-click → Extract All, run `TibiaMirror.exe`.
  5. **Quick start: your first mirror** (numbered, 5 steps, with `docs/images/add-region.gif` placeholder).
  6. **Features**: Mirrors (add, move, resize, lock, rename, opacity, show/hide all), Countdown bar, Audio timers, Profiles (incl. sharing via Export/Import), each 3–6 bullets.
  7. **Is this safe / allowed?** — "TibiaMirror only shows a live picture of your Tibia window (the same Windows feature that powers the taskbar previews) and listens for the hotkeys you choose. It never reads or changes the game, never presses keys for you and never blocks your keys." Advise users to follow Tibia's rules and that they use it at their own risk.
  8. **FAQ / troubleshooting**: mirrors are black or missing (Tibia minimized; Tibia must be running; run both at the same permission level); hotkeys don't work (Tibia must be the active window; Listening must be ON; another app may use the key); multiple monitors; where settings live (`%APPDATA%\TibiaMirror`, "Open data folder" button); how to move settings to a new PC (Export/Import); uninstall (Windows Settings → Apps; delete `%APPDATA%\TibiaMirror` to remove settings).
  9. **For developers**: prerequisites (.NET 8 SDK), `dotnet build`, `dotnet test`, run, project layout, how releases are made (`git tag v1.2.3 && git push --tags`).
  10. Credits & license (MIT; sounds are original and synthesized; not affiliated with CipSoft or TibiaVision; "Tibia" is a trademark of CipSoft GmbH).

- [ ] **Step 6: CHANGELOG.md** (Keep a Changelog format) with `## [0.1.0] - unreleased` listing the features. **docs/manual-test-checklist.md**: the checklist from spec §8 as checkboxes, grouped: Detection, Mirrors, Multi-monitor & DPI, Profiles, Audio timers, Countdown bar, Persistence, Installer/Portable.
- [ ] **Step 7: Verify** README links resolve (relative paths exist), `dotnet build` and tests green.
- [ ] **Step 8: Commit** — `git commit -m "build: installer, release workflow, README and docs"`

---

## Orchestrator finish (not a worker task)

- Run the full `docs/manual-test-checklist.md` against real Tibia (needs the user at the keyboard for in-game hotkey checks).
- Take README screenshots/GIF with the user's consent (they show the user's game).
- Whole-branch review on `opus`, then ask the user before creating the GitHub repo, pushing, and tagging `v0.1.0`.
