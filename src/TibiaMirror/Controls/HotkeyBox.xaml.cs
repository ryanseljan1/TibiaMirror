using System.Windows;
using System.Windows.Controls;
using TibiaMirror.Abstractions;
using TibiaMirror.Hotkeys;
using TibiaMirror.Localization;
using TibiaMirror.Models;

namespace TibiaMirror.Controls;

public partial class HotkeyBox : UserControl
{
    /// <summary>Set by AppHost. Null when hotkeys are unavailable.</summary>
    public static HotkeyService? Service { get; set; }
    public static IKeyboardSource? Source { get; set; }

    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey), typeof(Hotkey), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HotkeyBox)d).UpdateText()));

    public static readonly RoutedEvent HotkeyChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(HotkeyChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HotkeyBox));

    public Hotkey? Hotkey { get => (Hotkey?)GetValue(HotkeyProperty); set => SetValue(HotkeyProperty, value); }
    public event RoutedEventHandler HotkeyChanged { add => AddHandler(HotkeyChangedEvent, value); remove => RemoveHandler(HotkeyChangedEvent, value); }

    bool _recording;
    IKeyboardSource? _recordingSource;
    Window? _recordingWindow;

    public HotkeyBox()
    {
        InitializeComponent();
        LostFocus += (_, _) => StopRecording();
        LostKeyboardFocus += (_, _) => StopRecording();
        Unloaded += (_, _) => StopRecording();
        UpdateText();
    }

    void UpdateText() => Btn.Content = _recording ? Loc.Get("Settings_HotkeyPrompt") : Hotkey?.ToString() ?? Loc.Get("Settings_HotkeyNone");

    void Btn_Click(object sender, RoutedEventArgs e)
    {
        if (_recording) return;
        var service = Service; var source = Source;
        if (service is null || source is null) { Btn.Content = Loc.Get("Settings_HotkeyUnavailable"); return; }
        _recording = true;
        service.IsSuspended = true;
        _recordingSource = source;
        source.KeyDown += OnRecordedKey;
        _recordingWindow = Window.GetWindow(this);
        if (_recordingWindow is not null) _recordingWindow.Deactivated += OnWindowDeactivated;
        Focus();
        UpdateText();
    }

    void OnRecordedKey(KeyPress press)
    {
        // Hook callback: hop to the UI thread.
        Dispatcher.BeginInvoke(() =>
        {
            if (!_recording) return;
            bool changed = false;
            if (press.VirtualKey == 0x1B) { /* Esc cancels */ }
            else if (press.VirtualKey is 0x08 or 0x2E) { Hotkey = null; changed = true; }
            else { Hotkey = new Hotkey(press.VirtualKey, press.Modifiers); changed = true; }
            StopRecording();
            if (changed) RaiseEvent(new RoutedEventArgs(HotkeyChangedEvent, this));
        });
    }

    void OnWindowDeactivated(object? sender, EventArgs e) => StopRecording();

    void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        if (_recordingSource is not null) _recordingSource.KeyDown -= OnRecordedKey;
        _recordingSource = null;
        if (_recordingWindow is not null) _recordingWindow.Deactivated -= OnWindowDeactivated;
        _recordingWindow = null;
        if (Service is { } s) s.IsSuspended = false;
        UpdateText();
    }
}
