# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.1.0] - unreleased

### Added
- Tibia detection with a status dot (waiting, found, minimized).
- Mirrors: select any part of the Tibia window and show it live in its own window. Move, resize, lock (click-through), hide, rename, change opacity, re-crop and delete. Show/Hide all, with a hotkey.
- Option to show mirrors only while Tibia is focused.
- Audio timers: global hotkeys (active while Tibia is focused) start a timer and play a sound when it ends. Built-in sounds or your own audio files, per-timer and master volume, Listening on/off switch.
- Countdown bar: a bar on one side of a mirror that shrinks over a set time, with color, thickness, full-edge or narrow-strip shape, optional border flash and end sound.
- Profiles: create, rename, duplicate, delete, switch (UI or hotkey), export and import to share a setup.
- Settings window with a data folder shortcut, Tibia process/title options, and an app-wide dark mode.
- Automatic saving, restore of window positions on start, and multi-monitor and mixed-DPI handling.
- System tray icon, single instance, and application icon.
- Installer (Inno Setup) and portable zip, built by a GitHub release workflow.
- Deleting a mirror, timer or profile happens at once with an Undo button for 5 seconds, instead of a confirmation box.
- Add Region and Re-crop are disabled while Tibia is missing, with a hint instead of a message box.
- Mirrors no longer flicker when you alt-tab quickly: they hide only after Tibia has been unfocused for about 0.6 s, and new mirrors stay visible for 2 s.
- Mouse wheel over an unlocked mirror zooms it from 0.5x to 4x.
- Right-click menu on unlocked mirrors (Re-crop, Glow, Lock, Hide, Delete).
- Glow border per mirror: on/off, 7 colours and 3 intensities.
- Hotkey to lock or unlock all mirrors.
- "Start with Windows" option in Settings; at sign-in the app starts in the tray.
- Audio timers: repeat a sound every N seconds until stopped, running timers continue after a restart, m4a/aac/flac sounds, and "From preset" timers.
