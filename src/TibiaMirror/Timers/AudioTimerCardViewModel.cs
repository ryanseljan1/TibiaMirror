using CommunityToolkit.Mvvm.ComponentModel;
using TibiaMirror.Abstractions;
using TibiaMirror.Audio;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

public sealed class AudioTimerCardViewModel : ObservableObject
{
    readonly AudioTimerService _service;
    string _remainingText = "Ready";
    readonly bool _soundMissing;

    public AudioTimerCardViewModel(AudioTimerModel model, AudioTimerService service, ISoundPlayer sounds)
    {
        Model = model; _service = service;
        _soundMissing = !sounds.CanPlay(model.Sound);
        Refresh();
    }

    public AudioTimerModel Model { get; }
    public string Name => Model.Name;
    public string HotkeyText => Model.Hotkey?.ToString() ?? "No key";
    public string DurationText => $"{Model.DurationSeconds:0.##} s";
    public string RemainingText { get => _remainingText; private set => SetProperty(ref _remainingText, value); }
    public bool SoundMissing => _soundMissing;
    public string WarningText => _soundMissing
        ? $"\u26A0 Sound file not found \u2014 using {BuiltInSounds.All.First(s => s.Id == BuiltInSounds.DefaultId).DisplayName}"
        : "";

    public void Refresh()
    {
        var rem = _service.Remaining(Model.Id);
        RemainingText = rem is { } r ? $"{r.TotalSeconds:0.0} s" : "Ready";
    }
}
