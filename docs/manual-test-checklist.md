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
- [ ] "Only show mirrors while Tibia is focused" hides mirrors when you alt-tab away.
- [ ] Settings window: empty process name or title prefix is rejected, Save works, "Open data folder" opens the folder.
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
- [ ] Custom mp3 sound plays. A missing mp3 shows a warning and plays the Chime.
- [ ] Test button plays the sound. Master and per-timer volume work.
- [ ] HotkeyBox: alt-tab while capturing cancels cleanly.
- [ ] Assigning a key already in use shows the conflict prompt.

## Countdown bar
- [ ] Each side (left, right, top, bottom) with both shapes (Full edge, Narrow strip) draws correctly.
- [ ] The bar follows a mirror while you drag it.
- [ ] Pressing the hotkey again restarts the countdown.
- [ ] When the time ends: the border flashes 5 times and the end sound plays.
- [ ] A hidden mirror hides its bar.
- [ ] Tibia not focused: the countdown hotkey is ignored.
- [ ] Switching profile closes the bars.
- [ ] Deleting a mirror during a countdown removes its bar.

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
