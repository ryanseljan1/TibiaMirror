using TibiaMirror.Abstractions;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

public sealed class AudioTimerService(IClock clock, ISoundPlayer sounds)
{
    readonly Dictionary<Guid, DateTime> _endsAt = new();
    Profile _profile = new();

    public event Action<Guid>? Completed;

    public void Load(Profile profile) { _profile = profile; _endsAt.Clear(); }

    AudioTimerModel? Find(Guid id) => _profile.Timers.FirstOrDefault(t => t.Id == id);

    public void Trigger(Guid timerId)
    {
        if (!_profile.IsListening) return;
        var t = Find(timerId);
        if (t is null) return;
        if (_endsAt.ContainsKey(timerId) && t.OnRepress == RepressBehavior.IgnoreWhileRunning) return;
        _endsAt[timerId] = clock.UtcNow.AddSeconds(t.DurationSeconds);
    }

    public void Cancel(Guid timerId) => _endsAt.Remove(timerId);

    public void CancelAll() => _endsAt.Clear();

    public bool IsRunning(Guid id) => _endsAt.ContainsKey(id);

    public TimeSpan? Remaining(Guid id) =>
        _endsAt.TryGetValue(id, out var end) ? TimeSpan.FromTicks(Math.Max(0, (end - clock.UtcNow).Ticks)) : null;

    public void Tick()
    {
        var now = clock.UtcNow;
        var due = _endsAt.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList();
        foreach (var id in due)
        {
            _endsAt.Remove(id);
            var t = Find(id);
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
