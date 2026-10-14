using TibiaMirror.Audio;

namespace TibiaMirror.Countdown;

public static class CountdownSoundWarning
{
    /// <summary>Warning text for a countdown whose configured sound cannot be played (playback falls back to the default chime); "" if fine or no sound.</summary>
    public static string For(string? soundId, Func<string, bool> canPlay) =>
        string.IsNullOrEmpty(soundId) || canPlay(soundId)
            ? ""
            : $"\u26A0 Sound file not found \u2014 using {BuiltInSounds.All.First(s => s.Id == BuiltInSounds.DefaultId).DisplayName}";
}
