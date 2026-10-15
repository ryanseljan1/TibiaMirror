using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaMirror.Abstractions;

namespace TibiaMirror.Controls;

/// <summary>
/// Backs the in-window "&lt;thing&gt; deleted - Undo" snackbar. One at a time: a new one replaces the old,
/// whose deletion simply becomes final. Time-driven via <see cref="Tick"/> (called from the UI timer).
/// </summary>
public sealed partial class SnackbarService(IClock clock) : ObservableObject
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);

    Action? _undo;
    DateTime _expiresAt;

    [ObservableProperty] string _message = "";
    [ObservableProperty] bool _isVisible;

    /// <summary>Shows <paramref name="message"/> with an Undo button that runs <paramref name="undo"/>.</summary>
    public void Show(string message, Action undo)
    {
        _undo = undo;
        _expiresAt = clock.UtcNow + Duration;
        Message = message;
        IsVisible = true;
    }

    /// <summary>Hides an expired snackbar.</summary>
    public void Tick()
    {
        if (IsVisible && clock.UtcNow >= _expiresAt) Dismiss();
    }

    /// <summary>Hides the snackbar without undoing (the deletion becomes final).</summary>
    public void Dismiss()
    {
        _undo = null;
        IsVisible = false;
    }

    [RelayCommand]
    public void Undo()
    {
        var undo = _undo;
        var expired = clock.UtcNow >= _expiresAt;
        Dismiss();   // hide first: the undo action may itself raise events that would otherwise dismiss us
        if (undo is null || expired) return;
        try { undo(); }
        catch (Exception e) { TibiaMirror.Logging.Log.Error("Undo failed", e); }
    }
}
