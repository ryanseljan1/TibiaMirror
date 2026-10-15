using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TibiaMirror.Abstractions;
using TibiaMirror.Logging;

namespace TibiaMirror.Audio;

public sealed class SoundPlayer : ISoundPlayer, IDisposable
{
    private readonly object _gate = new();
    private readonly HashSet<WaveOutEvent> _active = new();
    private bool _disposed;

    public bool CanPlay(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId)) return false;
        if (soundId.StartsWith("builtin:", StringComparison.Ordinal)) return BuiltInSounds.IsKnown(soundId);
        return SoundFormats.IsSupported(soundId) && File.Exists(soundId);
    }

    public void Play(string soundId, double volume)
    {
        if (_disposed) return;
        try { PlayCore(soundId, volume); }
        catch (Exception ex)
        {
            Log.Warn($"Could not play sound '{soundId}': {ex.Message}");
            if (soundId == BuiltInSounds.DefaultId) return;
            try { PlayCore(BuiltInSounds.DefaultId, volume); }
            catch (Exception ex2) { Log.Warn($"Could not play default sound: {ex2.Message}"); }
        }
    }

    private void PlayCore(string soundId, double volume)
    {
        if (!CanPlay(soundId)) throw new ArgumentException("Sound not available");
        WaveStream reader = soundId.StartsWith("builtin:", StringComparison.Ordinal)
            ? new WaveFileReader(new MemoryStream(BuiltInSounds.RenderWav(soundId)))
            : SoundFormats.UsesMediaFoundation(soundId) ? new MediaFoundationReader(soundId) : new AudioFileReader(soundId);
        WaveOutEvent? output = null;
        try
        {
            var sampleProvider = new VolumeSampleProvider(reader.ToSampleProvider())
            { Volume = (float)Math.Clamp(volume, 0, 1) };
            var o = output = new WaveOutEvent();
            o.PlaybackStopped += (_, _) =>
            {
                lock (_gate) _active.Remove(o);
                o.Dispose();
                reader.Dispose();
            };
            o.Init(sampleProvider);
            lock (_gate)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(SoundPlayer));
                _active.Add(o);
            }
            o.Play();
        }
        catch
        {
            if (output != null) { lock (_gate) _active.Remove(output); output.Dispose(); }
            reader.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        WaveOutEvent[] outs;
        lock (_gate) { _disposed = true; outs = _active.ToArray(); }
        foreach (var o in outs) { try { o.Stop(); } catch (Exception ex) { Log.Warn($"Stopping sound failed: {ex.Message}"); } }
    }
}
