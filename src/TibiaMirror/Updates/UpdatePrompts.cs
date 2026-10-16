using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TibiaMirror.Controls;
using TibiaMirror.Localization;
using TibiaMirror.Logging;

namespace TibiaMirror.Updates;

/// <summary>UI glue for DIST-01: first-run opt-in info bar, update snackbar, download progress. Create on the UI thread.</summary>
public sealed class UpdatePrompts
{
    readonly AppHost _host;
    readonly ContentControl _infoBarHost;
    readonly Dispatcher _ui = Application.Current.Dispatcher;

    public UpdatePrompts(AppHost host, ContentControl infoBarHost)
    {
        _host = host;
        _infoBarHost = infoBarHost;
        _host.Updates.UpdateAvailable += (_, r) => _ui.BeginInvoke(() => ShowAvailable(r));
    }

    public void Start()
    {
        if (_host.Settings.CheckForUpdates is null) ShowOptIn();
        else _ = CheckSafe();
    }

    async Task CheckSafe()
    {
        try { await _host.Updates.CheckAsync(); }
        catch (Exception e) { Log.Error("Update check failed", e); }
    }

    void ShowOptIn()
    {
        var turnOn = new Button { Content = Loc.Get("Updates_TurnOn"), Style = (Style)Application.Current.FindResource("Button.Primary"), Margin = new Thickness(12, 0, 0, 0) };
        var notNow = new Button { Content = Loc.Get("Updates_NotNow"), Style = (Style)Application.Current.FindResource("Button.Secondary"), Margin = new Thickness(8, 0, 0, 0) };
        var icon = new TextBlock { Style = (Style)Application.Current.FindResource("Glyph"), Text = (string)Application.Current.FindResource("Glyph.Info"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        var text = new TextBlock { Text = Loc.Get("Updates_OptInPrompt"), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
        DockPanel.SetDock(notNow, Dock.Right); DockPanel.SetDock(turnOn, Dock.Right);
        var panel = new DockPanel { Margin = new Thickness(12, 8, 12, 8) };
        DockPanel.SetDock(icon, Dock.Left);
        panel.Children.Add(icon); panel.Children.Add(notNow); panel.Children.Add(turnOn); panel.Children.Add(text);
        var bar = new Border { Child = panel };
        bar.SetResourceReference(Border.BackgroundProperty, "Brush.InfoSubtle");
        _infoBarHost.Content = bar;
        turnOn.Click += (_, _) => { Choose(true); _ = CheckSafe(); };
        notNow.Click += (_, _) => Choose(false);
    }

    void Choose(bool enabled)
    {
        _host.Settings.CheckForUpdates = enabled;
        try { _host.SettingsService?.Save(); }
        catch (Exception e) { Log.Error("Saving update preference failed", e); }
        _infoBarHost.Content = null;
    }

    void ShowAvailable(ReleaseInfo r)
    {
        var p = UpdateDecisions.Available(_host.Updates.IsPortable);
        _host.Snackbar.Show(Loc.Format(p.MessageKey, r.Version), Build(p, r), null);
    }

    List<SnackbarAction> Build(UpdatePrompt p, ReleaseInfo r) => p.Actions
        .Select(k => new SnackbarAction(Loc.Get(UpdateDecisions.ActionLabelKey(k)), () => Run(k, r))).ToList();

    void Run(UpdateActionKind kind, ReleaseInfo r)
    {
        switch (kind)
        {
            case UpdateActionKind.WhatsNew:
            case UpdateActionKind.Download:
                OpenUrl(r.HtmlUrl);
                if (kind == UpdateActionKind.WhatsNew) ShowAvailable(r);   // opening notes should not lose the prompt
                break;
            case UpdateActionKind.Skip:
                _host.Settings.SkippedVersion = r.Version.ToString();
                try { _host.SettingsService?.Save(); }
                catch (Exception e) { Log.Error("Saving skipped version failed", e); }
                break;
            case UpdateActionKind.UpdateNow:
            case UpdateActionKind.Retry:
                _ = Install(r);
                break;
        }
    }

    static void OpenUrl(string url)
    {
        try { if (url.Length > 0) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception e) { Log.Error("Could not open URL", e); }
    }

    async Task Install(ReleaseInfo r)
    {
        var sb = _host.Snackbar;
        sb.Show(Loc.Get("Updates_Downloading"), [], null);
        sb.IsBusy = true;
        var progress = new Progress<double>(v => sb.Progress = v);   // captures the UI context
        try { await _host.Updates.DownloadAndInstallAsync(r, progress); }
        catch (Exception e)
        {
            Log.Error("Update download failed", e);
            var p = UpdateDecisions.Failure(e);
            sb.Show(Loc.Get(p.MessageKey), Build(p, r), null);
        }
    }
}
