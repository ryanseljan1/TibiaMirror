using CommunityToolkit.Mvvm.ComponentModel;
using TibiaMirror.Abstractions;
using TibiaMirror.Audio;
using TibiaMirror.Countdown;
using TibiaMirror.Localization;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

public sealed class AudioTimerCardViewModel : ObservableObject
{
    readonly AudioTimerService _service;
    readonly SoundAvailabilityCache _availability;
    string _remainingText = Loc.Get("Timers_Ready");
    string _warning = "";

    public AudioTimerCardViewModel(AudioTimerModel model, AudioTimerService service, ISoundPlayer sounds, Func<DateTime>? now = null)
    {
        Model = model; _service = service;
        _availability = new SoundAvailabilityCache(sounds.CanPlay, now);
        Refresh();
    }

    public AudioTimerModel Model { get; }
    public string Name => Model.Name;
    public string HotkeyText => Model.Hotkey?.ToString() ?? Loc.Get("Timers_NoKey");
    public string DurationText => Loc.Format("Timers_Seconds", Model.DurationSeconds);
    public bool IsRepeat => Model.Repeat;
    public string LoopText => Loc.Format("Timers_LoopEvery", Model.EffectiveIntervalSeconds);
    public bool IsRunning => _service.IsRunning(Model.Id);
    public string RemainingText { get => _remainingText; private set => SetProperty(ref _remainingText, value); }
    public bool SoundMissing => _warning.Length > 0;
    /// <summary>Same wording as the countdown editor; empty when the sound can play.</summary>
    public string WarningText => _warning;

    public void Refresh()
    {
        var rem = _service.Remaining(Model.Id);
        RemainingText = rem is { } r ? Loc.Format("Timers_RemainingSeconds", r.TotalSeconds) : Loc.Get("Timers_Ready");
        OnPropertyChanged(nameof(IsRunning));
        // Re-evaluated on every UI tick (cached per path for ~2 s) so the warning follows the file appearing or going away.
        var warning = CountdownSoundWarning.For(Model.Sound, _availability.CanPlay);
        if (warning != _warning)
        {
            _warning = warning;
            OnPropertyChanged(nameof(WarningText));
            OnPropertyChanged(nameof(SoundMissing));
        }
    }
}
