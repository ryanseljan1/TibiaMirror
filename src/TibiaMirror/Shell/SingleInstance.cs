namespace TibiaMirror.Shell;
public sealed class SingleInstance : IDisposable
{
    const string MutexName = @"Local\TibiaMirror.SingleInstance";
    const string EventName = @"Local\TibiaMirror.ShowExisting";
    readonly Mutex _mutex; readonly EventWaitHandle _show; RegisteredWaitHandle? _wait;
    public bool IsFirst { get; }
    public SingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out var created); IsFirst = created;
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
    }
    public void SignalFirstInstance() => _show.Set();
    public void ListenForShowRequests(Action onShow) =>
        _wait = ThreadPool.RegisterWaitForSingleObject(_show, (_, _) => onShow(), null, Timeout.Infinite, false);
    public void Dispose() { _wait?.Unregister(null); if (IsFirst) _mutex.ReleaseMutex(); _mutex.Dispose(); _show.Dispose(); }
}
