using System.Text.Json.Serialization;

namespace TibiaMirror.Models;

public enum RepressBehavior { Restart, IgnoreWhileRunning }

public sealed class AudioTimerModel
{
    /// <summary>Shortest duration/interval (the edit dialog's minimum); also keeps interval maths away from zero.</summary>
    public const double MinSeconds = 0.1;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Timer";
    public Hotkey? Hotkey { get; set; }
    public double DurationSeconds { get; set; } = 10;
    public string Sound { get; set; } = "builtin:chime";
    public int Volume { get; set; } = 100;
    public RepressBehavior OnRepress { get; set; } = RepressBehavior.Restart;

    /// <summary>Loop: after the first fire (DurationSeconds) the sound repeats every interval until stopped.</summary>
    public bool Repeat { get; set; }

    /// <summary>Loop interval; null means "same as DurationSeconds".</summary>
    public double? IntervalSeconds { get; set; }

    /// <summary>
    /// Runtime state, persisted so a running timer survives a restart: the UTC end time of a one-shot timer, or the
    /// (last known) next fire time of a loop. Null when idle.
    /// </summary>
    public DateTime? ResumeAtUtc { get; set; }

    [JsonIgnore] public double EffectiveIntervalSeconds => Math.Max(MinSeconds, IntervalSeconds is > 0 ? IntervalSeconds.Value : DurationSeconds);
}
