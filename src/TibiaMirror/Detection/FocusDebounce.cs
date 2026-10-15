using TibiaMirror.Abstractions;

namespace TibiaMirror.Detection;

/// <summary>
/// Smooths the "only while Tibia focused" rule: showing is immediate, hiding waits until focus has been
/// lost continuously for <see cref="HideDelay"/>, and never happens inside the <see cref="Grace"/> window
/// that follows creating or re-cropping a region. Pure and clock-driven; callers re-poll at <see cref="NextCheck"/>.
/// </summary>
public sealed class FocusDebounce(IClock clock)
{
    public static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(600);
    public static readonly TimeSpan Grace = TimeSpan.FromSeconds(2);

    bool _shown;
    DateTime? _lostAt;
    DateTime _graceUntil = DateTime.MinValue;

    /// <summary>Forgets any pending hide and adopts the given focus state as the shown state.</summary>
    public void Reset(bool focused) { _shown = focused; _lostAt = null; }

    /// <summary>Call after a region was created or re-cropped; mirrors stay shown for <see cref="Grace"/>.</summary>
    public void NotifyRegionChanged()
    {
        _graceUntil = clock.UtcNow + Grace;
        _shown = true;
    }

    /// <summary>Maps the raw focus state to the effective one.</summary>
    public bool Filter(bool focused)
    {
        var now = clock.UtcNow;
        if (focused) { _shown = true; _lostAt = null; return true; }
        if (!_shown) return false;
        _lostAt ??= now;
        if (now >= HideAt(_lostAt.Value)) { _shown = false; _lostAt = null; }
        return _shown;
    }

    /// <summary>Time until a pending hide takes effect, or null if nothing is pending.</summary>
    public TimeSpan? NextCheck
    {
        get
        {
            if (!_shown || _lostAt is not { } lost) return null;
            var left = HideAt(lost) - clock.UtcNow;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }
    }

    DateTime HideAt(DateTime lostAt)
    {
        var due = lostAt + HideDelay;
        return due > _graceUntil ? due : _graceUntil;
    }
}
