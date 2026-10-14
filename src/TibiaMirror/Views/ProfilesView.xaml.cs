using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TibiaMirror.Profiles;

namespace TibiaMirror.Views;

public partial class ProfilesView : UserControl
{
    const string Filter = "TibiaMirror profile (*.json)|*.json";
    ProfilesViewModel? _vm;
    AppHost? _host;

    public ProfilesView() { InitializeComponent(); }

    ProfileService Service => _vm!.Service;
    Window? Owner => Window.GetWindow(this);
    string? SelectedName => (List.SelectedItem as ProfileItem)?.Name;

    /// <summary>The ProfileService only exists after AppHost.Start(), which raises ProfileChanged; bind lazily on that.</summary>
    public void Init(AppHost host)
    {
        _host = host;
        host.ProfileChanged += _ => Dispatcher.Invoke(Bind);
        Bind();
    }

    void Bind()
    {
        if (_host?.Profiles is not { } svc) return;
        if (_vm == null) { _vm = new ProfilesViewModel(svc); List.ItemsSource = _vm.Items; }
        else _vm.Refresh();
    }

    void Run(Action action)
    {
        if (_vm == null) return;
        try { action(); }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        { MessageBox.Show(Owner!, e.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { _vm.Refresh(); }
    }

    void New_Click(object s, RoutedEventArgs e) => Run(() =>
    {
        var n = ProfileNameDialog.Ask(Owner, "New profile", "", Service.List());
        if (n != null) Service.Create(n);
    });

    void Duplicate_Click(object s, RoutedEventArgs e) => Run(() =>
    {
        if (SelectedName is not { } src) return;
        var n = ProfileNameDialog.Ask(Owner, "Duplicate profile", src + " copy", Service.List());
        if (n != null) Service.Duplicate(src, n);
    });

    void Rename_Click(object s, RoutedEventArgs e) => Run(() =>
    {
        if (SelectedName is not { } old) return;
        var n = ProfileNameDialog.Ask(Owner, "Rename profile", old, Service.List(), renamingFrom: old);
        if (n != null) Service.Rename(old, n);
    });

    void Delete_Click(object s, RoutedEventArgs e) => Run(() =>
    {
        if (SelectedName is not { } n) return;
        if (MessageBox.Show(Owner!, $"Delete profile '{n}'? This can't be undone.", AppInfo.Name,
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Service.Delete(n);
    });

    void Switch_Click(object s, RoutedEventArgs e) => Run(() => { if (SelectedName is { } n) Service.Switch(n); });

    void List_DoubleClick(object s, System.Windows.Input.MouseButtonEventArgs e) =>
        Run(() => { if (SelectedName is { } n) Service.Switch(n); });

    void Import_Click(object s, RoutedEventArgs e) => Run(() =>
    {
        var dlg = new OpenFileDialog { Filter = Filter };
        if (dlg.ShowDialog(Owner) == true) Service.Import(dlg.FileName);
    });

    void Export_Click(object s, RoutedEventArgs e) => Run(() =>
    {
        if (SelectedName is not { } n) return;
        var dlg = new SaveFileDialog { Filter = Filter, FileName = n + ".json" };
        if (dlg.ShowDialog(Owner) == true) Service.Export(n, dlg.FileName);
    });
}
