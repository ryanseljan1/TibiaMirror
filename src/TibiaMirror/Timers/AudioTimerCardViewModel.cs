using CommunityToolkit.Mvvm.ComponentModel;
using TibiaMirror.Abstractions;
using TibiaMirror.Countdown;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

public sealed class AudioTimerCardViewModel : ObservableObject
{
    readonly AudioTimerService _service;
    string _remainingText = "Ready";
    readonly string _warning;

    public AudioTimerCardViewModel(AudioTimerModel model, AudioTimerService service, ISoundPlayer sounds)
    {
        Model = model; _service = service;
        _warning = CountdownSoundWarning.For(model.Sound, sounds.CanPlay);
        Refresh();
    }

    public AudioTimerModel Model { get; }
    public string Name => Model.Name;
    public string HotkeyText => Model.Hotkey?.ToString() ?? "No key";
    public string DurationText => $"{Model.DurationSeconds:0.##} s";
    public bool IsRepeat => Model.Repeat;
    public string LoopText => $"↻ every {Model.EffectiveIntervalSeconds:0.##} s";
    public bool IsRunning => _service.IsRunning(Model.Id);
    public string RemainingText { get => _remainingText; private set => SetProperty(ref _remainingText, value); }
    public bool SoundMissing => _warning.Length > 0;
    /// <summary>Same wording as the countdown editor; empty when the sound can play.</summary>
    public string WarningText => _warning;

    public void Refresh()
    {
        var rem = _service.Remaining(Model.Id);
        RemainingText = rem is { } r ? $"{r.TotalSeconds:0.0} s" : "Ready";
        OnPropertyChanged(nameof(IsRunning));
    }
}
