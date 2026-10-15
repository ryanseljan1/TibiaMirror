using TibiaMirror.Models; using TibiaMirror.Persistence; using TibiaMirror.Timers;
namespace TibiaMirror.Tests.Timers;

public class TimerLoopAndResumeTests
{
    readonly FakeClock _clock = new(); readonly FakeSounds _snd = new();
    readonly AudioTimerModel _t = new() { DurationSeconds = 10, Sound = "builtin:ding", Volume = 100 };
    AudioTimerService Make(bool listening = true)
    {
        var p = new Profile { IsListening = listening }; p.Timers.Add(_t);
        var s = new AudioTimerService(_clock, _snd); s.Load(p); return s;
    }
    AudioTimerService Reload(Profile p) { var s = new AudioTimerService(_clock, _snd); s.Load(p); return s; }

    [Fact] public void Loop_fires_every_interval_until_cancelled()
    {
        _t.Repeat = true; _t.IntervalSeconds = 5;
        var s = Make(); s.Trigger(_t.Id);
        _clock.Advance(10); s.Tick(); Assert.Single(_snd.Played);
        _clock.Advance(5); s.Tick(); Assert.Equal(2, _snd.Played.Count);
        _clock.Advance(5); s.Tick(); Assert.Equal(3, _snd.Played.Count);
        Assert.True(s.IsRunning(_t.Id));
        s.Cancel(_t.Id); _clock.Advance(30); s.Tick(); Assert.Equal(3, _snd.Played.Count);
    }

    [Fact] public void Loop_interval_defaults_to_duration()
    {
        _t.Repeat = true;
        var s = Make(); s.Trigger(_t.Id);
        _clock.Advance(10); s.Tick(); _clock.Advance(10); s.Tick();
        Assert.Equal(2, _snd.Played.Count);
    }

    [Fact] public void Loop_after_a_long_stall_plays_once_not_a_burst()
    {
        _t.Repeat = true; _t.IntervalSeconds = 2;
        var s = Make(); s.Trigger(_t.Id);
        _clock.Advance(100); s.Tick();
        Assert.Single(_snd.Played);
        Assert.True(s.Remaining(_t.Id)!.Value.TotalSeconds is > 0 and <= 2);
    }

    [Fact] public void Loop_restart_policy_restarts_countdown()
    {
        _t.Repeat = true; _t.IntervalSeconds = 5;
        var s = Make(); s.Trigger(_t.Id); _clock.Advance(6); s.Trigger(_t.Id);
        Assert.True(s.IsRunning(_t.Id)); Assert.Equal(10, s.Remaining(_t.Id)!.Value.TotalSeconds, 2);
    }

    [Fact] public void Loop_ignore_policy_repress_stops_the_loop()
    {
        _t.Repeat = true; _t.OnRepress = RepressBehavior.IgnoreWhileRunning;
        var s = Make(); s.Trigger(_t.Id); Assert.True(s.IsRunning(_t.Id));
        s.Trigger(_t.Id); Assert.False(s.IsRunning(_t.Id));
        s.Trigger(_t.Id); Assert.True(s.IsRunning(_t.Id));   // and starts again on the next press
    }

    [Fact] public void Non_loop_ignore_policy_repress_still_ignored()
    {
        _t.OnRepress = RepressBehavior.IgnoreWhileRunning;
        var s = Make(); s.Trigger(_t.Id); s.Trigger(_t.Id); Assert.True(s.IsRunning(_t.Id));
    }

    [Fact] public void Trigger_and_cancel_raise_StateChanged_and_track_ResumeAtUtc()
    {
        var s = Make(); var n = 0; s.StateChanged += () => n++;
        s.Trigger(_t.Id); Assert.Equal(1, n); Assert.Equal(_clock.UtcNow.AddSeconds(10), _t.ResumeAtUtc);
        s.Cancel(_t.Id); Assert.Equal(2, n); Assert.Null(_t.ResumeAtUtc);
    }

    [Fact] public void Completed_one_shot_clears_ResumeAtUtc()
    {
        var s = Make(); s.Trigger(_t.Id); _clock.Advance(11); s.Tick(); Assert.Null(_t.ResumeAtUtc);
    }

    [Fact] public void Running_timer_resumes_after_restart()
    {
        var s = Make(); s.Trigger(_t.Id);
        _clock.Advance(4);
        var p = new Profile { IsListening = true }; p.Timers.Add(_t);
        var s2 = Reload(p);
        Assert.True(s2.IsRunning(_t.Id)); Assert.Equal(6, s2.Remaining(_t.Id)!.Value.TotalSeconds, 2);
        _clock.Advance(7); s2.Tick(); Assert.Single(_snd.Played);
    }

    [Fact] public void Timer_that_ended_while_closed_is_dropped_silently()
    {
        var s = Make(); s.Trigger(_t.Id);
        _clock.Advance(3600);
        var p = new Profile { IsListening = true }; p.Timers.Add(_t);
        var s2 = Reload(p); s2.Tick();
        Assert.False(s2.IsRunning(_t.Id)); Assert.Empty(_snd.Played); Assert.Null(_t.ResumeAtUtc);
    }

    [Fact] public void Loop_resumes_at_next_interval_boundary_without_burst()
    {
        _t.Repeat = true; _t.IntervalSeconds = 10;
        var s = Make(); s.Trigger(_t.Id);           // first fire at +10
        _clock.Advance(95);                          // closed for 95 s: boundaries at 10..90 were missed; the next is 100
        var p = new Profile { IsListening = true }; p.Timers.Add(_t);
        var s2 = Reload(p); s2.Tick();
        Assert.Empty(_snd.Played); Assert.Equal(5, s2.Remaining(_t.Id)!.Value.TotalSeconds, 2);
        _clock.Advance(5); s2.Tick(); Assert.Single(_snd.Played);
    }

    [Fact] public void Not_listening_profile_does_not_resume()
    {
        var s = Make(); s.Trigger(_t.Id);
        var p = new Profile { IsListening = false }; p.Timers.Add(_t);
        Assert.False(Reload(p).IsRunning(_t.Id));
    }

    [Fact] public void ResumeAtUtc_survives_json_roundtrip_as_utc()
    {
        _clock.UtcNow = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var s = Make(); s.Trigger(_t.Id);
        var p = new Profile(); p.Timers.Add(_t);
        var json = System.Text.Json.JsonSerializer.Serialize(p, JsonStore.Options);
        var back = System.Text.Json.JsonSerializer.Deserialize<Profile>(json, JsonStore.Options)!;
        Assert.Equal(_t.ResumeAtUtc, back.Timers[0].ResumeAtUtc); Assert.Equal(DateTimeKind.Utc, back.Timers[0].ResumeAtUtc!.Value.Kind);
    }

    [Fact] public void Pre_change_timer_json_loads_with_safe_defaults()
    {
        const string old = """
        { "SchemaVersion": 1, "Name": "Old", "Regions": [], "IsListening": true, "MasterVolume": 70,
          "Timers": [ { "Id": "0b2f6a52-1c3e-4a55-9d3a-3a8c1f4e7b10", "Name": "Haste", "Hotkey": null,
            "DurationSeconds": 22.5, "Sound": "builtin:rise", "Volume": 60, "OnRepress": "IgnoreWhileRunning" } ] }
        """;
        var p = System.Text.Json.JsonSerializer.Deserialize<Profile>(old, JsonStore.Options)!;
        var t = p.Timers[0];
        Assert.Equal("Haste", t.Name); Assert.Equal(22.5, t.DurationSeconds); Assert.Equal(RepressBehavior.IgnoreWhileRunning, t.OnRepress);
        Assert.False(t.Repeat); Assert.Null(t.IntervalSeconds); Assert.Null(t.ResumeAtUtc); Assert.Equal(22.5, t.EffectiveIntervalSeconds);
        var s = new AudioTimerService(_clock, _snd); s.Load(p); Assert.False(s.IsRunning(t.Id));
    }

    [Fact] public void Normalizer_drops_invalid_interval()
    {
        var p = new Profile(); p.Timers.Add(new AudioTimerModel { Repeat = true, IntervalSeconds = -3 });
        TibiaMirror.Profiles.ProfileNormalizer.Normalize(p);
        Assert.Null(p.Timers[0].IntervalSeconds);
    }

    [Fact] public void Presets_are_valid_and_have_no_hotkey()
    {
        Assert.Equal(5, TimerPresets.All.Count);
        foreach (var pr in TimerPresets.All)
        {
            var m = pr.ToModel();
            Assert.Null(m.Hotkey); Assert.True(TibiaMirror.Audio.BuiltInSounds.IsKnown(m.Sound));
            Assert.InRange(m.DurationSeconds, 0.1, 3600); Assert.Equal(pr.Repeat, m.Repeat);
            Assert.NotEqual(Guid.Empty, m.Id);
        }
        Assert.NotEqual(TimerPresets.All[0].ToModel().Id, TimerPresets.All[0].ToModel().Id);
        Assert.Contains(TimerPresets.All, p => p.Repeat);
    }
}
