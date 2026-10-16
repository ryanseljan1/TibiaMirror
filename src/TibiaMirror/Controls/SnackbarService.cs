using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaMirror.Abstractions;

namespace TibiaMirror.Controls;

/// <summary>A button on the snackbar; running it dismisses the snackbar first.</summary>
public sealed record SnackbarAction(string Label, Action Run);

/// <summary>
/// Backs the in-window snackbar ("&lt;thing&gt; deleted - Undo", update prompts, ...). One at a time: a new one replaces
/// the old. Time-driven via <see cref="Tick"/> (called from the UI timer); a null duration makes it sticky.
/// </summary>
public sealed partial class SnackbarService(IClock clock) : ObservableObject
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);

    DateTime? _expiresAt;

    [ObservableProperty] string _message = "";
    [ObservableProperty] bool _isVisible;
    [ObservableProperty] bool _isBusy;
    /// <summary>0..1 for a determinate progress bar; null shows none (or indeterminate when <see cref="IsBusy"/>).</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsIndeterminate))] double? _progress;
    public bool IsIndeterminate => Progress is null;

    public ObservableCollection<SnackbarAction> Actions { get; } = [];

    /// <summary>Shows <paramref name="message"/> with an Undo button that runs <paramref name="undo"/>.</summary>
    public void Show(string message, Action undo) =>
        Show(message, [new SnackbarAction(TibiaMirror.Localization.Loc.Get("Common_Undo"), undo)], Duration);

    /// <summary>Shows a message with any number of actions; <paramref name="duration"/> null = stays until dismissed.</summary>
    public void Show(string message, IReadOnlyList<SnackbarAction> actions, TimeSpan? duration)
    {
        Actions.Clear();
        foreach (var a in actions) Actions.Add(a);
        _expiresAt = duration is { } d ? clock.UtcNow + d : null;
        IsBusy = false;
        Progress = null;
        Message = message;
        IsVisible = true;
    }

    /// <summary>Hides an expired snackbar.</summary>
    public void Tick()
    {
        if (IsVisible && IsExpired) Dismiss();
    }

    /// <summary>Hides the snackbar without running any action (a deletion becomes final).</summary>
    [RelayCommand]
    public void Dismiss()
    {
        Actions.Clear();
        IsBusy = false;
        Progress = null;
        IsVisible = false;
    }

    bool IsExpired => _expiresAt is { } e && clock.UtcNow >= e;

    [RelayCommand]
    public void RunAction(SnackbarAction? action)
    {
        var expired = IsExpired;
        var known = action is not null && Actions.Contains(action);
        Dismiss();   // hide first: the action may itself raise events that would otherwise dismiss us
        if (!known || expired) return;
        try { action!.Run(); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error($"Snackbar action '{action!.Label}' failed", e); }
    }
}
