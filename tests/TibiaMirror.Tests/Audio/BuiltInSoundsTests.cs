using System.IO;
using System.Linq;
using System.Collections.Generic;
using TibiaMirror.Audio;
namespace TibiaMirror.Tests.Audio;

public class BuiltInSoundsTests
{
    [Fact] public void Six_sounds_with_unique_ids() { Assert.Equal(6, BuiltInSounds.All.Count); Assert.Equal(6, BuiltInSounds.All.Select(s => s.Id).Distinct().Count()); }

    [Theory][MemberData(nameof(Ids))]
    public void Each_renders_valid_short_pcm_wav(string id)
    {
        var wav = BuiltInSounds.RenderWav(id);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(wav, 8, 4));
        using var r = new NAudio.Wave.WaveFileReader(new MemoryStream(wav));
        Assert.Equal(44100, r.WaveFormat.SampleRate);
        Assert.InRange(r.TotalTime.TotalSeconds, 0.1, 1.5);
    }
    public static IEnumerable<object[]> Ids() => BuiltInSounds.All.Select(s => new object[] { s.Id });

    [Fact] public void Unknown_id_throws() => Assert.Throws<ArgumentException>(() => BuiltInSounds.RenderWav("builtin:nope"));
}

public class SoundPlayerTests
{
    [Fact] public void CanPlay_rules()
    {
        using var p = new SoundPlayer();
        Assert.True(p.CanPlay("builtin:chime"));
        Assert.False(p.CanPlay("builtin:nope"));
        Assert.False(p.CanPlay(@"C:\definitely\missing.mp3"));
        var txt = Path.GetTempFileName(); try { Assert.False(p.CanPlay(txt)); } finally { File.Delete(txt); }
    }
}
