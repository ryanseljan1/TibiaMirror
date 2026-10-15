using TibiaMirror.Models;

namespace TibiaMirror.Timers;

/// <summary>Generic starting points for "New timer from preset"; the values are examples, not game data.</summary>
public sealed record TimerPreset(string Label, string Name, double DurationSeconds, string Sound, bool Repeat = false)
{
    /// <summary>A fresh unsaved timer (new Id, no hotkey) for the edit dialog to prefill from.</summary>
    public AudioTimerModel ToModel() => new()
    {
        Name = Name, DurationSeconds = DurationSeconds, Sound = Sound, Repeat = Repeat,
        IntervalSeconds = Repeat ? DurationSeconds : null,
    };
}

public static class TimerPresets
{
    public static IReadOnlyList<TimerPreset> All { get; } = new[]
    {
        new TimerPreset("Short buff (30 s)", "Short buff", 30, "builtin:ding"),
        new TimerPreset("Long buff (10 min)", "Long buff", 600, "builtin:chime"),
        new TimerPreset("Food (5 min, repeat)", "Food", 300, "builtin:low-beep", Repeat: true),
        new TimerPreset("Mana potion reminder (1 min, repeat)", "Mana potion reminder", 60, "builtin:double-ding", Repeat: true),
        new TimerPreset("Boss respawn check (20 min)", "Boss respawn check", 1200, "builtin:alert"),
    };
}
