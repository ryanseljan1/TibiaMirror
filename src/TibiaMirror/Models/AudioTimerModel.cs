namespace TibiaMirror.Models;

public enum RepressBehavior { Restart, IgnoreWhileRunning }

public sealed class AudioTimerModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Timer";
    public Hotkey? Hotkey { get; set; }
    public double DurationSeconds { get; set; } = 10;
    public string Sound { get; set; } = "builtin:chime";
    public int Volume { get; set; } = 100;
    public RepressBehavior OnRepress { get; set; } = RepressBehavior.Restart;
}
