# Manual test checklist

Run this against real Tibia before each release. Tick every box. Items marked (key) need you at the keyboard.

## Detection
- [ ] Start TibiaMirror before Tibia: the status shows "Waiting for Tibia" (white dot).
- [ ] Start Tibia: the dot turns green.
- [ ] Minimize Tibia: the dot turns yellow and mirrors show the "Can't mirror" state or hide.
- [ ] Restore Tibia: the dot and mirrors recover.
- [ ] Close and restart Tibia: TibiaMirror reconnects and mirrors work again.
- [ ] Tray: balloon text appears when expected, double-click brings the window to the front, tray Exit quits.
- [ ] Main window tabs (Mirrors, Audio Timers, Profiles) all open.

## Mirrors
- [ ] Add Region: the selector shows its hint. Esc or right-click cancels it.
- [ ] Drag a tiny region: "Too small" is shown and no mirror is created.
- [ ] Add two regions, including one that only partly overlaps the Tibia window (partial crop).
- [ ] Drag and resize a mirror; the picture stays correct.
- [ ] Move the Tibia window: mirrors still show the right area.
- [ ] Lock a mirror: clicks pass through it. Unlock: it can be moved again. "Lock all" works.
- [ ] Rename and delete a mirror. Re-crop a mirror. Use the eye button to hide and show one mirror.
- [ ] Opacity slider changes the mirror transparency.
- [ ] Show/Hide all works from the button and from its hotkey (key).
- [ ] "Only show mirrors while Tibia is focused" hides mirrors when you alt-tab to the app or another program; Tibia and a mirror window count as focused.
- [ ] A timer or countdown hotkey works immediately after focusing Tibia, and does nothing immediately after focusing TibiaMirror's main window, settings or a dialog (key).
- [ ] Settings window: empty process name or title prefix is rejected, Save works, "Open data folder" opens the folder.
- [ ] Settings: turn Dark mode on and off; the main window, tabs, controls and dialogs stay readable. Restart and confirm the saved theme returns.
- [ ] Assign either global hotkey to a timer, countdown or the other global hotkey: Settings names the conflict and lets you decline it (key).
- [ ] The "Hotkeys are unavailable." banner appears when hotkeys cannot start. Retry restores them, including audio-timer and countdown hotkeys.

## Multi-monitor & DPI
- [ ] Two monitors: a mirror can be placed on the second monitor and keeps its place after restart.
- [ ] A mirror on a monitor that is now disabled or unplugged moves to the primary monitor on start.
- [ ] 150% DPI: the selector, mirrors and main window are sized and cropped correctly.
- [ ] Mixed DPI: the main window position and size are restored correctly after restart.
- [ ] The main window position is restored after restart.

## Profiles
- [ ] New profile: the New dialog has OK disabled when it opens.
- [ ] Switch profile in the UI: mirrors, timers and countdowns change.
- [ ] Switch profile with the "Next profile" hotkey (key). The toast looks right and sits in the expected place.
- [ ] Rename, duplicate and delete a profile.
- [ ] Export a profile, then Import it back (also try on another profile name).
- [ ] Profile switch closes any running countdown bars.

## Audio timers
- [ ] F2 (or your key) in Tibia starts the timer; the same key in Notepad does nothing (key).
- [ ] Timer finishes and plays its sound; pressing the key again restarts it or is ignored, per its setting.
- [ ] Listening OFF: hotkeys do nothing and running timers are cancelled. Listening ON: they work again.
- [ ] "Start listening when the app opens" is respected after restart.
- [ ] With Listening ON, untick "Start listening when the app opens" and restart: Listening starts OFF. Switching profiles during a session preserves each profile's current Listening state.
- [ ] Custom mp3 sound plays. A missing mp3 shows a warning and plays the Chime.
- [ ] Test button plays the sound. Master and per-timer volume work.
- [ ] HotkeyBox: alt-tab while capturing cancels cleanly.
- [ ] Assigning a key already in use shows the conflict prompt.
- [ ] Switch profiles while a timer edit dialog is open, then confirm: the result does not modify either profile.

## Countdown bar
- [ ] Each side (left, right, top, bottom) with both shapes (Full edge, Narrow strip) draws correctly.
- [ ] The bar follows a mirror while you drag it.
- [ ] Pressing the hotkey again restarts the countdown.
- [ ] When the time ends: the border flashes 5 times and the end sound plays.
- [ ] A hidden mirror hides its bar.
- [ ] Tibia not focused: the countdown hotkey is ignored.
- [ ] Switching profile closes the bars.
- [ ] Deleting a mirror during a countdown removes its bar.
- [ ] Choose a missing custom countdown sound: the editor shows a warning and playback falls back to Chime.
- [ ] Switch profiles while the Add Region or Re-crop selector is open: confirming the selection does not modify the new profile.

## Persistence
- [ ] Close and restart the app: mirrors, timers, profiles and settings are restored.
- [ ] Edit something, kill the process 2 seconds later, relaunch: the state is restored.
- [ ] Log off or shut down while the app runs: state is saved and the app does not block session end.
- [ ] Corrupt a settings file on purpose: the app restores from the backup or starts with defaults, and keeps the bad file with a `.corrupt` ending.

## Installer / Portable
- [ ] Installer: run `TibiaMirror-Setup-X.Y.Z.exe`, finish setup, launch from the Start menu.
- [ ] Desktop shortcut and "Start when Windows starts" options work when ticked.
- [ ] Upgrade over an existing install keeps settings.
- [ ] Uninstall removes the app; `%APPDATA%\TibiaMirror` stays (delete it by hand to remove settings).
- [ ] Portable zip: extract, run `TibiaMirror.exe`, everything above works.
- [ ] The app and tray show the new icon.

## QoL pack (0.1.0)

### Delete and Undo
- [ ] Delete a mirror: it disappears at once and "… deleted · Undo" shows for about 5 s. Undo brings it back in the same place in the list, with the same settings, and its window reappears if it was visible.
- [ ] Delete a timer and a profile, then Undo each: they come back unchanged, and a deleted current profile becomes current again. The last profile still can't be deleted.
- [ ] Let the snackbar time out: the deletion is final. Delete two things in a row: only the latest can be undone.
- [ ] The snackbar is readable in dark and light mode.

### Waiting for Tibia
- [ ] With Tibia closed (or minimized), Add Region and Re-crop are disabled. Hovering shows "Open Tibia to start adding mirrors", and no message box appears.
- [ ] With Tibia closed, the Mirrors page shows the hint about the Tibia process/title setting. It disappears when Tibia starts.

### Focus debounce
- [ ] "Only show mirrors while Tibia is focused" on: a quick alt-tab away and back (under ½ s) doesn't make the mirrors flicker. Staying away hides them after about 0.6 s.
- [ ] Right after adding or re-cropping a region, the new mirror stays visible for about 2 s even though Tibia isn't focused.

### Wheel zoom
- [ ] Mouse wheel over an unlocked mirror scales it in 0.1 steps between 0.5× and 4× of the crop size, keeping the aspect ratio and the point under the cursor. The size is kept after restart.
- [ ] Mouse wheel over a locked mirror does nothing to the mirror (it scrolls whatever is under it).

### Lock-all hotkey
- [ ] Settings: assign a Lock/Unlock-all hotkey (key). Pressing it locks all mirrors if any visible one is unlocked; pressing it again unlocks all. Mirror cards update.
- [ ] Assigning a key already used by Show/Hide all, Next profile, a timer or a countdown shows the conflict prompt (key).

### Start with Windows
- [ ] Settings: tick "Start with Windows" and Save. `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\TibiaMirror` is `"<exe path>" --startup`. Untick and Save: the value is removed.
- [ ] Sign out and in (or reboot): TibiaMirror starts in the tray with no window shown. The tray icon opens the window.
- [ ] Installer with "Start when Windows starts" ticked: the Settings checkbox shows ticked, and no Startup-folder shortcut is created.

### Timers: loops, resume, formats, presets
- [ ] A timer with "Repeat every N s" plays after its duration and then every N s, and the card shows it is looping. The Stop button ends it.
- [ ] Re-press on a loop: "Restart" restarts it; "Ignore while running" stops it.
- [ ] Start a 2-minute timer and a loop, close the app, reopen within the time: both continue with the right remaining time. Close it past the end of a one-shot timer: it doesn't play late.
- [ ] Custom sounds in m4a, aac and flac play. A file that fails to play falls back to the Chime without a crash.
- [ ] "From preset" next to Add timer: each preset opens the edit dialog prefilled with name, duration and sound, and no hotkey.

### Mirror context menu and glow
- [ ] Right-click an unlocked mirror: the menu shows Re-crop, Glow, Lock, Hide and Delete, in the app's theme. Each does the same as the card's button, and Delete offers Undo. Re-crop is disabled while Tibia is missing.
- [ ] Right-click a locked mirror: no menu (the click goes to whatever is under it).
- [ ] Glow on/off, each colour preset and Low/Medium/High change the unlocked mirror's border. The colour chip on the card matches. The picture is never covered by the border.
- [ ] A locked mirror shows no border. A countdown end still flashes its border.
- [ ] Mirrors from an older settings file load with the default orange glow.

### Click-through (0.1.1)
- [ ] With Tibia focused, clicks and right-clicks over a locked mirror act in Tibia, and the mirror still shows the game picture.
- [ ] "Click through mirrors while Tibia is focused" on (default): with Tibia focused, clicks over an unlocked mirror act in Tibia. Focus TibiaMirror and the same mirror can be dragged, resized, wheel-zoomed and right-clicked.
- [ ] Setting off: unlocked mirrors catch clicks again even while Tibia is focused; locked mirrors stay click-through.

## Updater + foundation (0.2.0)

### Themes
- [ ] Settings > Theme (System / Dark / Light): after Save, the new theme applies to every open window at once, no restart.
- [ ] System follows a live Windows light/dark switch (Settings > Personalization > Colors) while the app is open.
- [ ] Windows High Contrast on: the app uses the high-contrast colours. Off: it returns to the normal theme.
- [ ] In Dark, Light and High Contrast every screen is readable: main tabs, Settings, dialogs, mirror context menu, tooltips and the snackbar.
- [ ] Overlays (mirrors, countdown bar, region selector, toast) look the same in every theme.
- [ ] Upgrade from a 0.1.x settings file: the old dark/light choice is kept.

### Icons
- [ ] All icons render (no boxes or missing glyphs) on Windows 10 and Windows 11.
- [ ] `TibiaMirror.exe --glyph-test` lists every glyph.

### Controls
- [ ] The keyboard focus ring shows only when navigating with Tab, not after a mouse click.
- [ ] Toggles, the segmented control, sliders and combo boxes are usable by keyboard (key).
- [ ] The "Lock all" button label follows the hotkey: Lock all and Unlock all swap as mirrors are locked or unlocked (key).
- [ ] A timer whose sound file is missing shows the missing-sound warning. Remove the file: it appears live. Restore it: it disappears live.

### Updater
Setup: build a fake feed, a `releases/latest`-style JSON with `file:///` asset URLs, a real installer and `SHA256SUMS.txt`. Launch with env `TM_UPDATE_FEED=<path to the JSON>`.
- [ ] First run with automatic checks unset: an opt-in bar asks "Check for TibiaMirror updates on GitHub at startup? No other data is sent." with Turn on / Not now. The choice persists and the bar is not shown again.
- [ ] Startup check with a newer version in the feed: the snackbar shows "TibiaMirror {version} is available." with What's new, Update now, Later and Skip this version.
- [ ] Skip this version: on the next start that version is not offered, but Settings > Check now still finds it.
- [ ] Later: the snackbar closes and nothing is skipped. The next start offers it again.
- [ ] Update now: "Downloading update…" with progress, then a silent install. The app restarts on the new version and settings and profiles are kept.
- [ ] Tampered installer (hash not matching `SHA256SUMS.txt`): "The download failed verification and was deleted.", the downloaded file is deleted, nothing is installed, and Retry / Later are offered.
- [ ] Portable build (run from the unzipped folder): "TibiaMirror {version} is available (portable build)." with What's new, Download, Later. No install happens.
- [ ] Offline (feed unreachable): no error is shown and startup is not delayed.
- [ ] "Check for updates automatically" off: no request is made at startup (no feed access).
- [ ] Settings > Check now with nothing newer: "You're up to date." With a newer version: "Update {version} available — see the main window."

### Release pipeline
- [ ] The GitHub release has `SHA256SUMS.txt`. `Get-FileHash -Algorithm SHA256` of the downloaded installer and zip matches the listed hashes.
