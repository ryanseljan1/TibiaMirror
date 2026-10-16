using TibiaMirror.Abstractions;
using TibiaMirror.Models;
using TibiaMirror.Timers;

namespace TibiaMirror.Tests.Timers;

public class AudioTimerCardViewModelTests
{
    sealed class Sounds(Func<string, bool> canPlay) : ISoundPlayer
    {
        public void Play(string id, double v) { }
        public bool CanPlay(string id) => canPlay(id);
    }

    static AudioTimerCardViewModel Card(string sound, Func<string, bool> canPlay)
    {
        var sounds = new Sounds(canPlay);
        var model = new AudioTimerModel { Name = "T", Sound = sound };
        return new AudioTimerCardViewModel(model, new AudioTimerService(new FakeClock(), sounds), sounds);
    }

    [Fact]
    public void Missing_custom_sound_shows_the_countdown_warning_wording()
    {
        var c = Card(@"C:\nope\missing.mp3", _ => false);
        Assert.True(c.SoundMissing);
        Assert.StartsWith("Sound file not found", c.WarningText);
        Assert.Contains("using", c.WarningText);
    }

    [Fact]
    public void Playable_sound_has_no_warning()
    {
        var c = Card("builtin:chime", _ => true);
        Assert.False(c.SoundMissing);
        Assert.Equal("", c.WarningText);
    }

    [Fact]
    public void Service_exposes_sound_availability_for_the_edit_dialog()
    {
        var svc = new AudioTimerService(new FakeClock(), new Sounds(id => id == "ok"));
        Assert.True(svc.CanPlay("ok"));
        Assert.False(svc.CanPlay("missing"));
    }
}

public class AudioTimerCardLiveWarningTests
{
    sealed class Sounds(Func<string, bool> canPlay) : ISoundPlayer
    {
        public void Play(string id, double v) { }
        public bool CanPlay(string id) => canPlay(id);
    }

    [Fact]
    public void Warning_follows_the_file_appearing_and_disappearing_without_rechecking_within_two_seconds()
    {
        var exists = false; var checks = 0; var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var sounds = new Sounds(_ => { checks++; return exists; });
        var model = new AudioTimerModel { Name = "T", Sound = @"C:\x\a.mp3" };
        var c = new AudioTimerCardViewModel(model, new AudioTimerService(new FakeClock(), sounds), sounds, () => now);
        Assert.True(c.SoundMissing);

        exists = true; now += TimeSpan.FromSeconds(1); c.Refresh();
        Assert.True(c.SoundMissing);   // cached
        Assert.Equal(1, checks);

        now += TimeSpan.FromSeconds(2); c.Refresh();
        Assert.False(c.SoundMissing);
        Assert.Equal("", c.WarningText);

        exists = false; now += TimeSpan.FromSeconds(3); c.Refresh();
        Assert.True(c.SoundMissing);
    }
}
