using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TibiaMirror.Audio;
using TibiaMirror.Hotkeys;
using TibiaMirror.Models;
using TibiaMirror.Timers;

namespace TibiaMirror.Countdown;

/// <summary>Per-mirror countdown settings; edits write straight to the CountdownSettings and call the change callback.</summary>
public partial class CountdownEditor : UserControl
{
    sealed record SoundChoice(string? Id, string Name);
    static readonly SoundChoice NoneChoice = new(null, "None");
    static readonly SoundChoice CustomChoice = new("", "Custom file…");
    static readonly string[] Swatches = { "#FFFF7F00", "#FFE53935", "#FF43A047", "#FF1E88E5", "#FFFFFFFF", "#FFFDD835" };

    readonly RegionModel _region;
    readonly CountdownSettings _s;
    readonly HotkeyService? _hotkeys;
    readonly Action _changed;
    readonly Func<string, bool>? _canPlay;
    SoundChoice? _lastChoice;
    bool _loading = true, _revert;

    public CountdownEditor(RegionModel region, HotkeyService? hotkeys, Action changed, Func<string, bool>? canPlay = null)
    {
        InitializeComponent();
        _region = region; _s = region.Countdown; _hotkeys = hotkeys; _changed = changed; _canPlay = canPlay;
        foreach (var side in Enum.GetValues<BarSide>()) SideBox.Items.Add(side);
        foreach (var hex in Swatches)
        {
            var b = new Button
            {
                Width = 22, Height = 22, Margin = new Thickness(0, 0, 4, 0), Tag = hex, ToolTip = hex,
                Background = (Brush)new BrushConverter().ConvertFromString(hex)!,
            };
            b.Click += (_, _) => { SetColor(hex); };
            SwatchPanel.Children.Add(b);
        }
        SoundBox.Items.Add(NoneChoice);
        foreach (var (id, name) in BuiltInSounds.All) SoundBox.Items.Add(new SoundChoice(id, name));
        SoundBox.Items.Add(CustomChoice);

        EnabledBox.IsChecked = _s.Enabled;
        KeyBox.Hotkey = _s.Hotkey;
        DurationBox.Text = _s.DurationSeconds.ToString("0.##", CultureInfo.CurrentCulture);
        SideBox.SelectedItem = _s.Side;
        (_s.Shape == BarShape.NarrowStrip ? NarrowRadio : FullRadio).IsChecked = true;
        ColorBox.Text = _s.Color;
        ThicknessSlider.Value = Math.Clamp(_s.Thickness, 2, 40);
        FlashBox.IsChecked = _s.FlashOnDone;
        SelectSound(_s.Sound);
        _loading = false;
    }

    void Notify() { if (!_loading) _changed(); }

    void Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _s.Enabled = EnabledBox.IsChecked == true;
        if (SideBox.SelectedItem is BarSide side) _s.Side = side;
        _s.Shape = NarrowRadio.IsChecked == true ? BarShape.NarrowStrip : BarShape.FullEdge;
        _s.FlashOnDone = FlashBox.IsChecked == true;
        Notify();
    }

    void KeyBox_HotkeyChanged(object sender, RoutedEventArgs e)
    {
        var hotkey = KeyBox.Hotkey;
        if (hotkey is not null && _hotkeys is not null)
        {
            var others = _hotkeys.FindConflicts(hotkey, new CountdownHotkeyOwner(_region));
            if (others.Count > 0)
            {
                var names = string.Join(", ", others.Select(o => o is AudioTimerModel t ? t.Name : o.ToString()));
                if (MessageBox.Show(Window.GetWindow(this), $"This key is already used by: {names}. Use it anyway?", "Key already in use",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    KeyBox.Hotkey = _s.Hotkey;
                    return;
                }
            }
        }
        _s.Hotkey = hotkey;
        Notify();
    }

    void DurationBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (TimerEditDialog.TryParseDuration(DurationBox.Text, out var secs) && secs >= 0.5)
        {
            if (secs != _s.DurationSeconds) { _s.DurationSeconds = secs; Notify(); }
        }
        DurationBox.Text = _s.DurationSeconds.ToString("0.##", CultureInfo.CurrentCulture);
    }

    void SetColor(string hex)
    {
        _s.Color = hex;
        ColorBox.Text = hex;
        Notify();
    }

    void ColorBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var text = ColorBox.Text.Trim();
        if (!text.StartsWith('#')) text = "#" + text;
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(text)!;
            SetColor(c.ToString()); // normalised #AARRGGBB
        }
        catch { ColorBox.Text = _s.Color; }
    }

    void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        _s.Thickness = (int)ThicknessSlider.Value;
        Notify();
    }

    void UpdateSoundWarning()
    {
        var text = _canPlay is null ? "" : CountdownSoundWarning.For(_s.Sound, _canPlay);
        SoundWarning.Text = text;
        SoundWarning.Visibility = text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    void SelectSound(string? id)
    {
        SoundChoice? found = string.IsNullOrEmpty(id)
            ? NoneChoice
            : SoundBox.Items.OfType<SoundChoice>().FirstOrDefault(c => c.Id == id && c != CustomChoice);
        if (found is null)
        {
            found = new SoundChoice(id, BuiltInSounds.IsKnown(id!) ? id! : Path.GetFileName(id!));
            SoundBox.Items.Insert(SoundBox.Items.Count - 1, found);
        }
        _revert = true;
        SoundBox.SelectedItem = found;
        _revert = false;
        _lastChoice = found;
        UpdateSoundWarning();
    }

    void SoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_revert || _loading || SoundBox.SelectedItem is not SoundChoice c) return;
        if (c == CustomChoice)
        {
            var dlg = new OpenFileDialog { Filter = "Audio (*.mp3;*.wav)|*.mp3;*.wav" };
            if (dlg.ShowDialog(Window.GetWindow(this)) == true) { SelectSound(dlg.FileName); c = (SoundChoice)SoundBox.SelectedItem; }
            else { _revert = true; SoundBox.SelectedItem = _lastChoice; _revert = false; return; }
        }
        _lastChoice = c;
        _s.Sound = c.Id;
        UpdateSoundWarning();
        Notify();
    }
}
