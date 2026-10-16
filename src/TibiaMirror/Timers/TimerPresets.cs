using TibiaMirror.Localization;
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
    public static IReadOnlyList<TimerPreset> All => new[]
    {
        new TimerPreset(Loc.Get("Timers_PresetShortBuff"), Loc.Get("Timers_PresetShortBuffName"), 30, "builtin:ding"),
        new TimerPreset(Loc.Get("Timers_PresetLongBuff"), Loc.Get("Timers_PresetLongBuffName"), 600, "builtin:chime"),
        new TimerPreset(Loc.Get("Timers_PresetFood"), Loc.Get("Timers_PresetFoodName"), 300, "builtin:low-beep", Repeat: true),
        new TimerPreset(Loc.Get("Timers_PresetMana"), Loc.Get("Timers_PresetManaName"), 60, "builtin:double-ding", Repeat: true),
        new TimerPreset(Loc.Get("Timers_PresetBoss"), Loc.Get("Timers_PresetBossName"), 1200, "builtin:alert"),
    };
}
