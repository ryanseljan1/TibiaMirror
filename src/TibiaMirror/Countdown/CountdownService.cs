using TibiaMirror.Abstractions;

namespace TibiaMirror.Countdown;

/// <summary>Tracks running per-region countdowns. Not thread-safe; used from the UI thread.</summary>
public sealed class CountdownService(IClock clock)
{
    sealed record Run(DateTime Start, double Duration);
    readonly Dictionary<Guid, Run> _runs = new();

    /// <summary>Raised once per run, from <see cref="Tick"/>.</summary>
    public event Action<Guid>? Completed;

    public void Start(Guid regionId, double durationSeconds) => _runs[regionId] = new Run(clock.UtcNow, Math.Max(0.001, durationSeconds));

    public void Stop(Guid id) => _runs.Remove(id);

    public bool IsRunning(Guid id) => _runs.ContainsKey(id);

    public IReadOnlyCollection<Guid> RunningIds => _runs.Keys.ToList();

    /// <summary>1.0 just started, 0.0 done; 0 when not running.</summary>
    public double Progress(Guid id)
    {
        if (!_runs.TryGetValue(id, out var r)) return 0;
        var elapsed = (clock.UtcNow - r.Start).TotalSeconds;
        return Math.Clamp(1.0 - elapsed / r.Duration, 0.0, 1.0);
    }

    public void Tick()
    {
        var now = clock.UtcNow;
        foreach (var id in _runs.Where(kv => (now - kv.Value.Start).TotalSeconds >= kv.Value.Duration).Select(kv => kv.Key).ToList())
        {
            _runs.Remove(id);
            Completed?.Invoke(id);
        }
    }
}
