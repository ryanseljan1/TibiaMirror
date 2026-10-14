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
    [Fact] public void Throwing_action_does_not_escape_flush_and_is_retried()
    {
        int n = 0;
        using var d = new Debouncer(TimeSpan.FromSeconds(10), () => { if (++n == 1) throw new IOException("locked"); });
        d.Trigger(); d.Flush(); Assert.Equal(1, n);
        d.Flush(); Assert.Equal(2, n);
        d.Flush(); Assert.Equal(2, n);
    }
    [Fact] public void Trigger_after_dispose_does_nothing()
    {
        int n = 0; var d = new Debouncer(TimeSpan.FromMilliseconds(50), () => n++);
        d.Dispose(); d.Trigger(); Thread.Sleep(200); d.Flush(); Assert.Equal(0, n);
    }
    [Fact] public void Action_may_wait_on_another_thread_calling_trigger_without_deadlock()
    {
        Debouncer? d = null; int n = 0;
        d = new Debouncer(TimeSpan.FromSeconds(10), () => { n++; var t = Task.Run(() => d!.Trigger()); Assert.True(t.Wait(2000)); });
        using (d) { d.Trigger(); d.Flush(); Assert.Equal(1, n); }
    }
}
