# TibiaMirror — Design Spec

Date: 2026-10-13 · Status: approved in brainstorming, pending written-spec review
Working name: **TibiaMirror** (placeholder; defined once in `Directory.Build.props` / `AppInfo.cs` so a rename is one change)

## 1. Purpose

A free, open-source Windows app that mirrors parts of the Tibia client anywhere on screen, with audio
timers, per-mirror countdown bars, and profiles. It is distributed to other players via GitHub Releases and
doubles as a learning project in reverse engineering and rebuilding.

It is a **clean-room re-implementation** of the behavior of TibiaVision (studied in the separate
`Tibia Vision REA` workspace). No TibiaVision code, text, assets, sounds, or branding are used.

**Non-goals (v1):** world map, notes, magnifier, character locator, cursor glow, "Link", OBS-projector
mirrors, mirror groups, screenshots ("Snap"), ads, licensing/premium, auto-update, localization,
code signing.

**Hard constraint — game safety:** the app only (a) displays DWM thumbnails of the Tibia window and
(b) *observes* keyboard input. It never reads/writes Tibia process memory, injects code, sends or blocks
input, or automates gameplay. The README states this plainly.

## 2. Success criteria

1. A non-technical player can go from the GitHub page to a running app in ≤ 3 steps using the README.
2. Launching `TibiaMirror.exe` works like TibiaVision.exe: one window, tray icon, single instance.
3. All six feature areas below work against the real Tibia client (`client.exe`) on a 1-monitor and a
   2-monitor setup, at 100% and 150% display scaling.
4. Settings survive restarts, Tibia restarts, and an app crash mid-save.
5. CI builds on every push; tagging `vX.Y.Z` publishes `TibiaMirror-Setup-X.Y.Z.exe` and
   `TibiaMirror-Portable-X.Y.Z.zip` to a GitHub Release without manual steps.

## 3. Technology

- C# / .NET 8, WPF, `net8.0-windows`, x64, self-contained publish.
- MVVM with `CommunityToolkit.Mvvm`.
- Audio: `NAudio` (MIT).
- Tests: xUnit.
- Installer: Inno Setup 6 (`installer/TibiaMirror.iss`), per-user or all-users install, Start-menu +
  optional desktop shortcut, uninstaller, optional "start with Windows".
- CI: GitHub Actions on `windows-latest`.
- License: MIT.

## 4. Repository layout

Repo root: `C:\Users\Ryan\Documents\TibiaMirror` (separate from the REA workspace; research material never
enters this repo).

```
TibiaMirror/
├─ src/TibiaMirror/
│  ├─ App.xaml(.cs)            startup, single-instance, tray, crash logging
│  ├─ AppInfo.cs               name, version, data-folder name (single rename point)
│  ├─ Interop/                 NativeMethods (user32, dwmapi), DwmThumbnail wrapper, KeyboardHook
│  ├─ Services/                TibiaLocator, MirrorService, HotkeyService, AudioTimerService,
│  │                           CountdownService, ProfileService, SettingsStore, SoundPlayer
│  ├─ Models/                  Region, CountdownSettings, AudioTimer, Hotkey, Profile, AppSettings
│  ├─ Geometry/                CropMath, DpiMath, MonitorClamp (pure, unit-tested)
│  ├─ Views/ + ViewModels/     MainWindow (tabs), MirrorWindow, CountdownBarWindow,
│  │                           RegionSelectorWindow, HotkeyRecorder, dialogs
│  └─ Assets/Sounds/, Assets/Icons/   original/generated only
├─ tests/TibiaMirror.Tests/
├─ installer/TibiaMirror.iss
├─ .github/workflows/build.yml, release.yml
├─ docs/images/                README screenshots
├─ docs/superpowers/specs/     this file
└─ README.md, LICENSE, CHANGELOG.md, Directory.Build.props
```

## 5. Components

Each service has one job and talks to others through events/interfaces; Windows-specific code is confined
to `Interop/` behind small interfaces so logic is testable.

| Unit | Responsibility | Depends on |
|---|---|---|
| `TibiaLocator` | Poll every 500 ms for the Tibia window; raise `Found`, `Lost`, `BoundsChanged`, `FocusChanged`, `MinimizedChanged`. | `IWindowEnumerator` (user32) |
| `MirrorService` | Own the set of `MirrorWindow`s for the active profile; create/destroy, attach/detach thumbnails on locator events, apply visibility/lock. | `TibiaLocator`, `DwmThumbnail` |
| `DwmThumbnail` | Thin wrapper: register, update source rect / dest rect / opacity / visible, unregister. | dwmapi |
| `HotkeyService` | Single `WH_KEYBOARD_LL` hook; observe key-downs (always passes through via `CallNextHookEx`); match against registered `Hotkey`s; dispatch on the UI thread. Game hotkeys fire only when `TibiaLocator.IsFocused`. | `KeyboardHook`, `TibiaLocator` |
| `AudioTimerService` | Timer state machine (Idle → Running → Done/Idle), retrigger policy, fire sound on completion. | `IClock`, `ISoundPlayer`, `HotkeyService` |
| `CountdownService` | Per-region countdown state; drives `CountdownBarWindow` progress and mirror border flash. | `IClock`, `HotkeyService`, `MirrorService` |
| `ProfileService` | List/create/duplicate/rename/delete/switch/import/export profiles; remember last profile. | `SettingsStore` |
| `SettingsStore` | Load/save JSON with debounce (1 s), atomic write (temp + replace) and `.bak`; corruption recovery. | file system |
| `SoundPlayer` | Play built-in or custom files via NAudio at a given volume; fallback to default chime. | NAudio |

## 6. Feature behavior

### 6.1 App shell
- `TibiaMirror.exe` opens the main window: tabs **Mirrors · Audio Timers · Profiles**; top bar with Tibia
  status (🟢 "Tibia detected" / ⚪ "Waiting for Tibia…"), current profile name, **Show/Hide all**,
  **Lock all**, settings gear.
- Close (X) minimizes to tray; tray menu: Show, Show/Hide mirrors, Exit. A first-time balloon explains this.
- Single instance via named mutex; second launch signals the first to show itself, then exits.
- Unhandled exceptions are logged to `%APPDATA%\TibiaMirror\logs\` and shown in a friendly dialog.

### 6.2 Tibia detection
- Match: visible top-level window whose owning process name is `client` (case-insensitive) and title starts
  with `Tibia`. Process name and title prefix are stored in `AppSettings` (advanced) in case CipSoft changes them.
- Mirrors attach on `Found`, hide on `Lost` and on `Minimized` (DWM cannot render minimized windows), and
  return automatically on restore/relaunch.
- Optional "Only show mirrors while Tibia is focused" (default off). Mirror windows count as "Tibia focused"
  for this purpose so clicking near a mirror does not flicker it.

### 6.3 Mirror regions
- **Add Region** → `RegionSelectorWindow` overlays the Tibia client area (dimmed); drag a rectangle; live size
  readout; Esc cancels; minimum 8×8 px. Result: a new region "Region N" whose mirror appears offset beside
  the crop at 1:1 size.
- Crop is stored in **Tibia client-area physical pixels**; mirror bounds in **screen physical pixels**.
  `CropMath` converts to the DWM source rect each time Tibia bounds change.
- `MirrorWindow`: borderless, topmost, no taskbar entry, hosts the thumbnail over its full client area.
  - Unlocked: drag to move, corner-drag to resize (aspect locked by default, Shift = free), thin accent border.
  - Locked: click-through (`WS_EX_TRANSPARENT | WS_EX_LAYERED`), no border.
- Per-region properties: `Name`, `Opacity` (0.1–1.0), `IsVisible`, `IsLocked`, `Countdown` (§6.6).
- Mirrors tab: card per region — editable name, 👁, 🔒, ✏ Re-crop, 🗑 Delete (confirm), ▸ Countdown settings.
- Global **Show/Hide all** hotkey (app-wide, works regardless of focus).

### 6.4 Persistence
- Data folder: `%APPDATA%\TibiaMirror\` → `settings.json` (app-wide), `profiles\<name>.json`, `last_profile`,
  `logs\`.
- Every change is saved after a 1 s debounce and on exit. Writes go to `*.tmp` then `File.Replace` with
  `*.bak` backup.
- Load failure → try `.bak` → else rename to `*.corrupt`, start that profile empty, notify the user.
- Each file has `SchemaVersion` (start at 1) for future migrations.
- On restore, window rects are clamped onto existing monitors (`MonitorClamp`); off-screen mirrors move to
  the primary monitor. Per-monitor-v2 DPI awareness is declared in the app manifest.

### 6.5 Profiles
- Profile = regions (incl. countdown settings) + audio timers + listening flags. App-wide settings (global
  hotkeys, startup options, detection overrides) are not per-profile.
- Profiles tab: list with current marked; **New**, **Duplicate**, **Rename**, **Delete** (confirm; last one
  can't be deleted), **Switch**, **Import…/Export…** (copies a `.json`).
- Optional global **Next profile** hotkey; switching shows a 1.5 s toast "Profile: <name>".
- Names: 1–40 chars, unique case-insensitively, invalid filename characters rejected in the dialog.
- First run creates "Default".

### 6.6 Countdown bar (per region)
- Settings: `Enabled`, `Hotkey`, `DurationSeconds` (0.5–3600), `Side` (Top/Bottom/Left/Right),
  `Shape` (FullEdge | NarrowStrip), `Color`, `Thickness` (2–40 px), `FlashOnDone` (default on, 5 pulses),
  optional `Sound`.
- Hotkey (while Tibia focused) → bar full, drains linearly to zero at ~60 fps; re-press restarts.
- Bar is a separate click-through, topmost `CountdownBarWindow` positioned against the mirror's chosen side
  and moved with it (a DWM thumbnail covers its host window entirely, so the bar can't share it). Flash is
  drawn on the mirror's border overlay the same way.
- Hidden when the mirror is hidden or Tibia isn't shown.

### 6.7 Audio timers (per profile)
- Timer: `Name`, `Hotkey` (recorder; modifiers allowed), `DurationSeconds` (0.1–3600), `Sound`
  (built-in id or file path; mp3/wav), `Volume` (0–100), `OnRepress` (Restart | IgnoreWhileRunning).
- Master **Listening** toggle + "Start listening when app opens" + master volume (effective volume =
  master × timer volume).
- Hotkey fires only while Tibia is focused. On completion the sound plays.
- Tab shows each timer with live remaining time while running, **Test sound**, edit, delete.
- Built-in sounds: ~6 short original tones generated for this project (chime, ding, double-ding, alert,
  low-beep, rise), shipped as WAV, credited in README.

### 6.8 Hotkeys (shared)
- `Hotkey` = virtual-key + modifiers (Ctrl/Alt/Shift/Win). Recorder captures next key-down; Esc cancels;
  Backspace clears.
- On assigning a combination already used elsewhere in the active profile or app-wide, warn and let the user
  keep both or cancel.
- Hook observes only; it never swallows keys.

## 7. Error handling summary

| Situation | Behavior |
|---|---|
| Tibia not running | Status ⚪, mirrors hidden, everything else usable |
| Profile/settings file corrupt | `.bak` restore → `*.corrupt` + empty profile + notice |
| Custom sound missing/unplayable | Fallback chime + warning icon on the timer/countdown |
| DWM thumbnail registration fails | Log, mirror shows "Can't mirror — is Tibia minimized?" placeholder |
| Keyboard hook fails to install | Banner: "Hotkeys unavailable" with retry; mirrors still work |
| Unhandled exception | Log + friendly dialog; settings already persisted by debounce |

## 8. Testing

- **Unit (xUnit, CI):** `CropMath`, `DpiMath`, `MonitorClamp`; `SettingsStore` round-trip, atomic-write and
  corruption recovery (temp dirs); `ProfileService` operations and name validation; `AudioTimerService` and
  `CountdownService` state machines with a fake `IClock`/`ISoundPlayer`; hotkey matching and conflict
  detection.
- **Manual checklist (per release, in `docs/manual-test-checklist.md`):** detection on/off/minimize/restart
  of Tibia; add/move/resize/lock/rename/delete mirror; moving the Tibia window; 2 monitors; 150% DPI;
  profile switch via UI and hotkey; audio timer start/restart/finish; countdown bar each side + flash;
  restart app and verify restore; installer install/uninstall; portable zip run.

## 9. Distribution & README

- `build.yml`: restore, build, test on every push/PR.
- `release.yml` on tag `v*`: `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`,
  zip → `TibiaMirror-Portable-X.Y.Z.zip`; compile Inno Setup → `TibiaMirror-Setup-X.Y.Z.exe`; create a
  GitHub Release with both and the CHANGELOG section.
- README (top to bottom): one-line pitch + screenshot/GIF · **Install in 3 steps** (Download Setup.exe from
  Releases → run it, incl. what to do at the SmartScreen "Windows protected your PC" prompt: *More info →
  Run anyway* → open TibiaMirror and start Tibia) · Portable alternative · Quick start (add your first
  mirror, with screenshots) · Feature guide (Mirrors, Countdown bar, Audio timers, Profiles) · FAQ (Is this
  allowed / safe? What it does and doesn't touch; mirrors are black; Tibia minimized; multiple monitors;
  where settings live; uninstall) · Building from source · Contributing · License & credits.

## 10. Build order (each step leaves a working build)

1. Repo skeleton, CI build, app shell (main window, tray, single instance, logging)
2. Tibia detection + status
3. Mirror regions (selector, mirror windows, show/hide, lock, rename, re-crop, delete)
4. Persistence
5. Profiles
6. Audio timers (+ hotkey service, sounds)
7. Countdown bar
8. Installer, release workflow, README with screenshots
