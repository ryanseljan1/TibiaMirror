# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.1.0] - 2026-10-14

First public release. Compared with the release candidates (rc.1, rc.2), this version adds the "QoL pack" below.

### New in this release
- **Undo instead of "Are you sure?"**: deleting a mirror, timer or profile happens at once, with an Undo button for 5 seconds.
- **Right-click menu on mirrors**: Re-crop, Glow, Lock, Hide and Delete right on an unlocked mirror.
- **Glow border**: choose a border colour (7 presets) and intensity (Low, Medium, High) per mirror, or turn it off. A colour chip on the card shows it. Locked mirrors stay borderless.
- **Mouse-wheel zoom**: scroll over an unlocked mirror to scale it from 0.5x to 4x. Smooth-scrolling touchpads are supported.
- **Lock/Unlock all hotkey**: one key to make every mirror click-through, or movable again.
- **Start with Windows** in Settings: TibiaMirror starts quietly in the tray when you sign in.
- **Repeating timers**: "Repeat every N s" plays a sound on a loop until you stop it, which is handy for potion and food reminders.
- **Timers survive a restart**: a running timer continues after you close and reopen the app. One that ended while the app was closed doesn't play late.
- **Timer presets**: "From preset" creates common timers with name, duration and sound filled in.
- **More sound formats**: custom sounds can be m4a, aac or flac, as well as mp3 and wav.

### Improved
- No more flicker when alt-tabbing with "Only show mirrors while Tibia is focused": mirrors hide only after Tibia has been unfocused for about 0.6 s, and a new mirror stays visible for 2 s.
- While Tibia isn't running, Add Region and Re-crop are disabled with a hint, instead of showing a message box.
- The status dot is a real coloured dot (grey, amber, green).
- Dark mode: toggle buttons, dropdowns and the Listening label are readable.
- A missing custom timer sound shows a warning on the timer and in its editor (it plays the Chime instead).

### Fixed
- The main window opens where you left it (its position was overwritten at start).
- The installer's "Start when Windows starts" option and the in-app setting now use the same entry, so the app no longer starts twice. Uninstalling removes it.
- Switching profiles no longer brings back timers that were running in the other profile.
- A timer with a zero duration in an imported profile no longer crashes the app.

### Included since the release candidates
- Mirrors of any part of the Tibia window: move, resize, lock (click-through), hide, rename, opacity, re-crop. Show/Hide all, with a hotkey. Unlimited mirrors.
- Audio timers with global hotkeys, built-in or custom sounds, and per-timer and master volume.
- Countdown bars on mirrors, with colour, shape, border flash and end sound.
- Profiles: create, rename, duplicate, delete, switch (UI or hotkey), export and import.
- Settings with Tibia detection options and dark mode. Automatic saving, multi-monitor and mixed-DPI support, tray icon.
- Installer and portable zip.
