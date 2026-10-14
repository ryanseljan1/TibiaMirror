using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Persistence;
public class DebouncerTests
{
    [Fact] public void Many_triggers_run_once_after_delay()
    {
        int n = 0; using var d = new Debouncer(TimeSpan.FromMilliseconds(100), () => Interlocked.Increment(ref n));
        for (int i = 0; i < 5; i++) d.Trigger();
        Thread.Sleep(400); Assert.Equal(1, n);
    }
    [Fact] public void Flush_runs_pending_immediately_and_only_once()
    {
        int n = 0; using var d = new Debouncer(TimeSpan.FromSeconds(10), () => n++);
        d.Trigger(); d.Flush(); d.Flush(); Assert.Equal(1, n);
    }
    [Fact] public void Flush_without_trigger_does_nothing()
    {
        int n = 0; using var d = new Debouncer(TimeSpan.FromSeconds(10), () => n++); d.Flush(); Assert.Equal(0, n);
    }
}
