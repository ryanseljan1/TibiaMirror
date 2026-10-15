using TibiaMirror.Abstractions;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

public sealed class AudioTimerService(IClock clock, ISoundPlayer sounds)
{
    readonly Dictionary<Guid, DateTime> _endsAt = new();
    Profile _profile = new();
    bool _loaded;   // false until the first Load: only app startup resumes persisted timers

    public event Action<Guid>? Completed;

    /// <summary>Raised when persisted run state changed (a timer started or stopped); the host schedules a save.</summary>
    public event Action? StateChanged;

    /// <summary>
    /// Loads a profile. At app startup (the first Load) timers whose persisted end is still in the future resume; one-shot
    /// timers that ended while the app was closed are dropped silently, and loops resume at the next interval boundary.
    /// Switching to a different profile later never resumes anything (the old profile was saved with its run state set).
    /// </summary>
    public void Load(Profile profile)
    {
        // a reload of the same profile keeps its running timers (they are rebuilt from ResumeAtUtc below)
        var same = ReferenceEquals(_profile, profile);
        if (!same) foreach (var old in _profile.Timers) old.ResumeAtUtc = null;
        var resume = same || !_loaded;
        _loaded = true;
        _profile = profile; _endsAt.Clear();
        var now = clock.UtcNow;
        foreach (var t in profile.Timers)
        {
            if (!resume || t.ResumeAtUtc is not { } at || !profile.IsListening) { t.ResumeAtUtc = null; continue; }
            if (t.Repeat) _endsAt[t.Id] = NextBoundary(at, t.EffectiveIntervalSeconds, now);
            else if (at > now) _endsAt[t.Id] = at;
            else { t.ResumeAtUtc = null; continue; }
            t.ResumeAtUtc = _endsAt[t.Id];
        }
    }

    /// <summary>First fire time strictly after <paramref name="now"/> on the grid anchor + k * interval (no catch-up burst).</summary>
    static DateTime NextBoundary(DateTime anchor, double intervalSeconds, DateTime now)
    {
        if (anchor > now) return anchor;
        var steps = Math.Floor((now - anchor).TotalSeconds / intervalSeconds) + 1;
        return anchor.AddSeconds(steps * intervalSeconds);
    }

    AudioTimerModel? Find(Guid id) => _profile.Timers.FirstOrDefault(t => t.Id == id);

    void Set(AudioTimerModel t, DateTime end) { _endsAt[t.Id] = end; t.ResumeAtUtc = end; }

    /// <summary>
    /// Hotkey press. Idle: start. Running: "restart" restarts the countdown (or the loop); "ignore" leaves a one-shot
    /// timer alone but stops a running loop.
    /// </summary>
    public void Trigger(Guid timerId)
    {
        if (!_profile.IsListening) return;
        var t = Find(timerId);
        if (t is null) return;
        if (_endsAt.ContainsKey(timerId) && t.OnRepress == RepressBehavior.IgnoreWhileRunning)
        {
            if (t.Repeat) Cancel(timerId);
            return;
        }
        Set(t, clock.UtcNow.AddSeconds(t.DurationSeconds));
        StateChanged?.Invoke();
    }

    public void Cancel(Guid timerId)
    {
        if (!_endsAt.Remove(timerId)) return;
        if (Find(timerId) is { } t) t.ResumeAtUtc = null;
        StateChanged?.Invoke();
    }

    public void CancelAll()
    {
        if (_endsAt.Count == 0) return;
        _endsAt.Clear();
        foreach (var t in _profile.Timers) t.ResumeAtUtc = null;
        StateChanged?.Invoke();
    }

    public bool IsRunning(Guid id) => _endsAt.ContainsKey(id);

    public TimeSpan? Remaining(Guid id) =>
        _endsAt.TryGetValue(id, out var end) ? TimeSpan.FromTicks(Math.Max(0, (end - clock.UtcNow).Ticks)) : null;

    public void Tick()
    {
        var now = clock.UtcNow;
        var due = _endsAt.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList();
        foreach (var id in due)
        {
            var t = Find(id);
            if (t is { Repeat: true }) Set(t, NextBoundary(_endsAt[id], t.EffectiveIntervalSeconds, now));   // skips missed beats
            else { _endsAt.Remove(id); if (t is not null) t.ResumeAtUtc = null; }
            if (t is not null) Play(t);
            Completed?.Invoke(id);
        }
    }

    public void TestSound(Guid id)
    {
        if (Find(id) is { } t) Play(t);
    }

    /// <summary>Plays a not-yet-saved timer's sound at its effective volume (used by the edit dialog).</summary>
    public void TestSound(string sound, int volume) => sounds.Play(sound, Effective(volume));

    void Play(AudioTimerModel t) => sounds.Play(t.Sound, Effective(t.Volume));

    double Effective(int volume) => _profile.MasterVolume / 100.0 * volume / 100.0;
}
