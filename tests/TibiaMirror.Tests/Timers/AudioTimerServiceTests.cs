using TibiaMirror.Abstractions; using TibiaMirror.Models; using TibiaMirror.Timers;
namespace TibiaMirror.Tests.Timers;

internal sealed class FakeClock : IClock { public DateTime UtcNow { get; set; } = new(2026, 1, 1); public void Advance(double s) => UtcNow = UtcNow.AddSeconds(s); }
internal sealed class FakeSounds : ISoundPlayer
{
    public List<(string Id, double Vol)> Played = new();
    public void Play(string id, double v) => Played.Add((id, v));
    public bool CanPlay(string id) => true;
}

public class AudioTimerServiceTests
{
    readonly FakeClock _clock = new(); readonly FakeSounds _snd = new();
    readonly AudioTimerModel _t = new() { DurationSeconds = 10, Sound = "builtin:ding", Volume = 50 };
    AudioTimerService Make(bool listening = true, int master = 80)
    {
        var p = new Profile { IsListening = listening, MasterVolume = master }; p.Timers.Add(_t);
        var s = new AudioTimerService(_clock, _snd); s.Load(p); return s;
    }

    [Fact] public void Plays_once_when_duration_elapses()
    {
        var s = Make(); s.Trigger(_t.Id);
        _clock.Advance(9.9); s.Tick(); Assert.Empty(_snd.Played);
        _clock.Advance(0.2); s.Tick(); s.Tick();
        Assert.Single(_snd.Played); Assert.Equal("builtin:ding", _snd.Played[0].Id); Assert.False(s.IsRunning(_t.Id));
    }

    [Fact] public void Effective_volume_is_master_times_timer() { var s = Make(master: 80); s.Trigger(_t.Id); _clock.Advance(11); s.Tick(); Assert.Equal(0.4, _snd.Played[0].Vol, 3); }

    [Fact] public void Not_listening_ignores_trigger() { var s = Make(listening: false); s.Trigger(_t.Id); Assert.False(s.IsRunning(_t.Id)); }

    [Fact] public void Restart_policy_resets_remaining()
    {
        var s = Make(); s.Trigger(_t.Id); _clock.Advance(6); s.Trigger(_t.Id);
        Assert.Equal(10, s.Remaining(_t.Id)!.Value.TotalSeconds, 2);
    }

    [Fact] public void Ignore_policy_keeps_remaining()
    {
        _t.OnRepress = RepressBehavior.IgnoreWhileRunning;
        var s = Make(); s.Trigger(_t.Id); _clock.Advance(6); s.Trigger(_t.Id);
        Assert.Equal(4, s.Remaining(_t.Id)!.Value.TotalSeconds, 2);
    }

    [Fact] public void Cancel_stops_without_sound() { var s = Make(); s.Trigger(_t.Id); s.Cancel(_t.Id); _clock.Advance(20); s.Tick(); Assert.Empty(_snd.Played); }

    [Fact] public void Completed_event_raised() { var s = Make(); Guid? done = null; s.Completed += id => done = id; s.Trigger(_t.Id); _clock.Advance(10); s.Tick(); Assert.Equal(_t.Id, done); }

    [Fact] public void Unknown_timer_id_is_ignored() { var s = Make(); s.Trigger(Guid.NewGuid()); s.Tick(); Assert.Empty(_snd.Played); }

    [Fact] public void Load_clears_running() { var s = Make(); s.Trigger(_t.Id); s.Load(new Profile { IsListening = true }); Assert.False(s.IsRunning(_t.Id)); }
}
