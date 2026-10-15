# TibiaMirror

[Português (Brasil)](README.md)

Mirror any part of your Tibia screen anywhere you want — health bars, cooldowns, the minimap — plus audio timers and countdown bars.

**Free and open source:** no ads, no license key, no network calls, unlimited mirrors — every feature included. It never reads game memory or sends keys or clicks.

> [!TIP]
> [![New release: v0.1.0](https://img.shields.io/badge/new%20release-v0.1.0-FF7F00?style=for-the-badge)](https://github.com/ryanseljan1/TibiaMirror/releases/tag/v0.1.0)
>
> **TibiaMirror 0.1.0 is out!** Undo for deletes, a right-click menu on mirrors, glow borders, mouse-wheel zoom, repeating timers and more. **[Read the patch notes →](https://github.com/ryanseljan1/TibiaMirror/releases/tag/v0.1.0)**

<!-- Screenshots are coming. Add docs/images/overview.png, then uncomment:
![TibiaMirror overview](docs/images/overview.png)
-->

## ⬇ Install (3 steps)

1. Go to the **[Releases page](../../releases)** and download `TibiaMirror-Setup-<version>.exe`.
2. Run it. If Windows shows "Windows protected your PC", click **More info → Run anyway**. TibiaMirror is free and not code-signed (a signing certificate costs money every year), so Windows does not know the publisher yet.
3. Open TibiaMirror from the Start menu, then start Tibia. The dot at the top turns green.

### No-install option

Download `TibiaMirror-Portable-<version>.zip` from the Releases page. Right-click it, choose **Extract All**, then run `TibiaMirror.exe`.

## Quick start: your first mirror

<!-- Screenshots are coming. Add docs/images/add-region.gif, then uncomment:
![Adding a region](docs/images/add-region.gif)
-->

1. Start Tibia and wait until the dot in TibiaMirror is green.
2. On the **Mirrors** tab, click **＋ Add Region**.
3. Drag a box around the part of Tibia you want to copy, for example your health bar. Press Esc or right-click to cancel.
4. A new mirror window appears. Drag it where you want it and pull its edges to resize it.
5. Click the lock (padlock) button on the mirror's card to make it click-through, so it never gets in your way.

## Features

### Mirrors
- Add as many regions as you like. Each one gets its own window and a card in the list.
- Move and resize a mirror freely. Use **Re-crop** to pick a different area.
- **Lock** makes a mirror click-through. **Lock all** locks every mirror at once.
- The eye button shows or hides one mirror, and the pencil renames it. **Delete** removes it. The Opacity slider makes a mirror see-through.
- **Show/Hide all** turns every mirror on or off. You can also set a hotkey for it in settings (⚙).
- Tick "Only show mirrors while Tibia is focused" in settings (⚙) to hide mirrors when you switch to another program. Tibia or one of your mirror windows being active counts as focused; TibiaMirror's own main window, dialogs and settings do not.
- Tick **Dark mode** in settings (⚙) and click **Save** to change the app theme. Your choice is restored when the app opens again.

### Countdown bar
- Each mirror can have a bar that shrinks over a time you choose. Open the **Countdown** section on the mirror's card and tick **Enable countdown**.
- Pick a hotkey. It only works while Tibia (or one of your mirror windows) is the active window, never while you type in TibiaMirror itself. If the key is already used by a timer, a countdown or a global hotkey, you are asked before it is accepted.
- Choose the side (left, right, top, bottom), the shape (**Full edge** or **Narrow strip**), the color and the thickness.
- The bar follows the mirror when you move it. Pressing the key again restarts it.
- When time is up, the border can flash and a sound can play.

### Audio timers
- On the **Audio Timers** tab, click **＋ Add Timer**. Pick a hotkey and a duration.
- Press the key in Tibia and the timer starts. When it ends, you hear a sound.
- Use a built-in sound or your own audio file. **Test** plays the sound.
- Choose what happens if you press the key while the timer runs: restart it, or ignore the key.
- **Listening: ON/OFF** switches all timer hotkeys on or off. There is also a master volume.
- "Start listening when the app opens" decides the Listening state each time the app starts: ticked means ON, unticked means OFF (whatever it was when you last closed the app).

### Profiles
- A profile is a full setup: mirrors, timers and countdowns. Use **New**, **Rename**, **Duplicate** and **Delete** on the **Profiles** tab, and **Switch** to change.
- Set a "Next profile" hotkey in settings (⚙) to switch fast. Settings warns you if a global hotkey clashes with a timer, a countdown or the other global hotkey. A small message shows the new profile name.
- **Export...** saves a profile to a file. **Import...** loads one. This is how you share a setup with a friend or move to a new PC.

## Is this safe / allowed?

TibiaMirror only shows a live picture of your Tibia window (the same Windows feature that powers the taskbar previews) and listens for the hotkeys you choose. It never reads or changes the game, never presses keys for you and never blocks your keys.

Please follow Tibia's rules and use TibiaMirror at your own risk.

## FAQ / troubleshooting

**My mirrors are black or missing.**
Tibia must be running and must not be minimized. Run TibiaMirror and Tibia at the same permission level (both normal, or both as administrator).

**My hotkeys do not work.**
Tibia (or one of your mirror windows) must be the active window; TibiaMirror's own window does not count. **Listening** must be ON on the Audio Timers tab. Another program may already use that key, so try a different one. If you see "Hotkeys are unavailable.", click **Retry**.

**I use more than one monitor.**
Mirrors can sit on any monitor. If a monitor is gone the next time you start the app, its mirrors move to your main monitor.

**Where are my settings?**
In `%APPDATA%\TibiaMirror`. Open the settings (⚙) and click **Open data folder** to get there.

**How do I move my settings to a new PC?**
On the Profiles tab, click **Export...** on the old PC and **Import...** on the new one.

**How do I uninstall?**
Windows Settings → Apps → TibiaMirror → Uninstall. Your settings are kept. Delete the folder `%APPDATA%\TibiaMirror` to remove them too. For the portable version, delete its folder.

## For developers

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

```
dotnet build TibiaMirror.sln -c Debug
dotnet test tests/TibiaMirror.Tests -c Debug
dotnet run --project src/TibiaMirror
```

Project layout:

- `src/TibiaMirror` — the WPF app (Mirrors, Timers, Countdown, Profiles, Settings, Hotkeys, Detection, Interop).
- `tests/TibiaMirror.Tests` — unit tests.
- `installer` — the Inno Setup script.
- `tools` — helper scripts (`make-icon.ps1` builds the app icon).
- `docs` — the manual test checklist and design notes.

To make a release, tag a version and push the tag. GitHub Actions builds, tests and publishes the installer and the portable zip.

```
git tag v1.2.3 && git push --tags
```

## Contributing

Issues and pull requests are welcome.

- Run `dotnet test tests/TibiaMirror.Tests` before you open a pull request, and add tests for new logic.
- Clean-room rule: do not copy code or assets from other tools. Write your own code, and keep sounds and images original.
- Game-safety rule: TibiaMirror must never read or write the game's memory, inject anything into it, send or post input to it, or block keys. Changes that do any of this will not be accepted.

## Credits & license

Released under the MIT license (see `LICENSE`). The sounds are original and synthesized. TibiaMirror is not affiliated with CipSoft. "Tibia" is a trademark of CipSoft GmbH.
