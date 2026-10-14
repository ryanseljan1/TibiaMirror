namespace TibiaMirror.Abstractions;
public interface ISoundPlayer
{
    /// <param name="soundId">"builtin:&lt;id&gt;" or an absolute file path.</param>
    /// <param name="volume">0.0 to 1.0</param>
    void Play(string soundId, double volume);
    bool CanPlay(string soundId);
}
