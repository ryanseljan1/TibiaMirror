namespace TibiaMirror.Audio;

/// <summary>Caches "can this sound play" per path so UI ticks can re-check cheaply; entries expire after <see cref="Ttl"/>.</summary>
public sealed class SoundAvailabilityCache
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(2);

    readonly Func<string, bool> _canPlay;
    readonly Func<DateTime> _now;
    readonly Dictionary<string, (bool Ok, DateTime At)> _entries = new(StringComparer.OrdinalIgnoreCase);

    public SoundAvailabilityCache(Func<string, bool> canPlay, Func<DateTime>? now = null)
    {
        _canPlay = canPlay; _now = now ?? (() => DateTime.UtcNow);
    }

    public bool CanPlay(string soundId)
    {
        var now = _now();
        if (_entries.TryGetValue(soundId, out var e) && now - e.At < Ttl) return e.Ok;
        var ok = _canPlay(soundId);
        _entries[soundId] = (ok, now);
        return ok;
    }
}
