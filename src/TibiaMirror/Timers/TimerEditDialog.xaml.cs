using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TibiaMirror.Abstractions;
using TibiaMirror.Audio;
using TibiaMirror.Hotkeys;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

public partial class TimerEditDialog : Window
{
    sealed record SoundChoice(string Id, string Name);
    static readonly SoundChoice CustomChoice = new("", "Custom file\u2026");

    readonly AudioTimerModel? _existing;
    readonly HotkeyService? _hotkeys;
    readonly AudioTimerService _service;
    SoundChoice? _lastChoice;
    bool _revert;

    /// <summary>The edited timer (a copy; keeps the existing Id when editing). Set when the user saved.</summary>
    public AudioTimerModel? Result { get; private set; }

    /// <param name="template">Prefill for a new timer (a preset); ignored when editing.</param>
    public TimerEditDialog(AudioTimerModel? existing, HotkeyService? hotkeys, AudioTimerService service, AudioTimerModel? template = null)
    {
        InitializeComponent();
        _existing = existing; _hotkeys = hotkeys; _service = service;
        Title = existing is null ? "Add timer" : "Edit timer";
        var m = existing ?? template ?? new AudioTimerModel { Name = "Timer" };
        NameBox.Text = m.Name;
        KeyBox.Hotkey = m.Hotkey;
        DurationBox.Text = m.DurationSeconds.ToString("0.##", CultureInfo.CurrentCulture);
        RepeatCheck.IsChecked = m.Repeat;
        IntervalBox.Text = m.EffectiveIntervalSeconds.ToString("0.##", CultureInfo.CurrentCulture);
        VolumeSlider.Value = m.Volume;
        (m.OnRepress == RepressBehavior.IgnoreWhileRunning ? IgnoreRadio : RestartRadio).IsChecked = true;
        foreach (var (id, name) in BuiltInSounds.All) SoundBox.Items.Add(new SoundChoice(id, name));
        SoundBox.Items.Add(CustomChoice);
        SelectSound(m.Sound);
    }

    void SelectSound(string id)
    {
        var found = SoundBox.Items.OfType<SoundChoice>().FirstOrDefault(c => c.Id == id && c != CustomChoice);
        if (found is null)
        {
            found = new SoundChoice(id, BuiltInSounds.IsKnown(id) ? id : Path.GetFileName(id));
            SoundBox.Items.Insert(SoundBox.Items.Count - 1, found);
        }
        SoundBox.SelectedItem = found;
        _lastChoice = found;
        UpdateSoundWarning(found);
    }

    void UpdateSoundWarning(SoundChoice c)
    {
        var text = Countdown.CountdownSoundWarning.For(c == CustomChoice ? null : c.Id, _service.CanPlay);
        SoundWarning.Text = text;
        SoundWarning.Visibility = text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    void SoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_revert || SoundBox.SelectedItem is not SoundChoice c) return;
        if (c != CustomChoice) { _lastChoice = c; UpdateSoundWarning(c); return; }
        var dlg = new OpenFileDialog { Filter = SoundFormats.DialogFilter };
        if (dlg.ShowDialog(this) == true) SelectSound(dlg.FileName);
        else { _revert = true; SoundBox.SelectedItem = _lastChoice; _revert = false; }
    }

    /// <summary>Accepts "1.5" and "1,5" (current culture, then invariant); range 0.1 to 3600.</summary>
    internal static bool TryParseDuration(string? text, out double seconds)
    {
        seconds = 0;
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return false;
        var styles = NumberStyles.Float;
        double v;
        if (!double.TryParse(text, styles, CultureInfo.CurrentCulture, out v) && !double.TryParse(text, styles, CultureInfo.InvariantCulture, out v))
        {
            // "1,5" under an en-US culture: treat the comma as a decimal separator.
            if (!double.TryParse(text.Replace(',', '.'), styles, CultureInfo.InvariantCulture, out v)) return false;
        }
        if (double.IsNaN(v) || v < 0.1 || v > 3600) return false;
        seconds = v;
        return true;
    }

    void Test_Click(object sender, RoutedEventArgs e)
    {
        if (SoundBox.SelectedItem is SoundChoice c && c != CustomChoice)
            _service.TestSound(c.Id, (int)VolumeSlider.Value);
    }

    void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length is < 1 or > 40) { ErrorText.Text = "Enter a name between 1 and 40 characters"; return; }
        if (!TryParseDuration(DurationBox.Text, out var secs)) { ErrorText.Text = "Enter a duration between 0.1 and 3600 seconds"; return; }
        var repeat = RepeatCheck.IsChecked == true;
        double interval = secs;
        if (repeat && !TryParseDuration(IntervalBox.Text, out interval)) { ErrorText.Text = "Enter a repeat interval between 0.1 and 3600 seconds"; return; }
        if (SoundBox.SelectedItem is not SoundChoice sound || sound == CustomChoice) { ErrorText.Text = "Pick a sound"; return; }
        var hotkey = KeyBox.Hotkey;
        if (hotkey is not null && _hotkeys is not null)
        {
            var others = _hotkeys.FindConflicts(hotkey, _existing);
            if (others.Count > 0)
            {
                var names = string.Join(", ", others.Select(o => o is AudioTimerModel t ? t.Name : o.ToString()));
                if (MessageBox.Show(this, $"This key is already used by: {names}. Use it anyway?", "Key already in use",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            }
        }
        Result = new AudioTimerModel
        {
            Id = _existing?.Id ?? Guid.NewGuid(),
            Name = name, Hotkey = hotkey, DurationSeconds = secs, Sound = sound.Id,
            Volume = (int)VolumeSlider.Value,
            Repeat = repeat, IntervalSeconds = repeat ? interval : null,
            OnRepress = IgnoreRadio.IsChecked == true ? RepressBehavior.IgnoreWhileRunning : RepressBehavior.Restart,
        };
        DialogResult = true;
    }
}
