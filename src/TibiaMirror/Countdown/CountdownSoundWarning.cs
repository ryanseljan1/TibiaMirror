using TibiaMirror.Audio;
using TibiaMirror.Localization;

namespace TibiaMirror.Countdown;

public static class CountdownSoundWarning
{
    /// <summary>Warning text for a countdown whose configured sound cannot be played (playback falls back to the default chime); "" if fine or no sound. The warning glyph is drawn by the view.</summary>
    public static string For(string? soundId, Func<string, bool> canPlay) =>
        string.IsNullOrEmpty(soundId) || canPlay(soundId)
            ? ""
            : Loc.Format("Timers_SoundMissing", BuiltInSounds.All.First(s => s.Id == BuiltInSounds.DefaultId).DisplayName);
}
