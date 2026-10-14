namespace TibiaMirror.Persistence;

public sealed class Debouncer : IDisposable
{
    readonly TimeSpan _delay;
    readonly Action _action;
    readonly Timer _timer;
    readonly object _lock = new();
    bool _pending;
    bool _disposed;

    public Debouncer(TimeSpan delay, Action action)
    {
        _delay = delay;
        _action = action;
        _timer = new Timer(_ => RunIfPending(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Trigger()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _pending = true;
            _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Flush() => RunIfPending();

    void RunIfPending()
    {
        lock (_lock)
        {
            if (!_pending) return;
            _pending = false;
        }
        try { _action(); }
        catch (Exception e)
        {
            Logging.Log.Error("Debounced action failed", e);
            lock (_lock) { if (!_disposed) _pending = true; }   // re-armed; next Trigger/Flush retries
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _pending = false;
            _timer.Dispose();
        }
    }
}
