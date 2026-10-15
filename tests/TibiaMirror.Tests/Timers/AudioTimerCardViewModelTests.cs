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
        Assert.StartsWith("\u26A0 Sound file not found", c.WarningText);
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
