namespace TibiaMirror.Models;

public sealed class Profile
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "Default";
    public List<RegionModel> Regions { get; set; } = new();
    public List<AudioTimerModel> Timers { get; set; } = new();
    public bool IsListening { get; set; }
    public bool StartListeningOnOpen { get; set; }
    public int MasterVolume { get; set; } = 80;
}
