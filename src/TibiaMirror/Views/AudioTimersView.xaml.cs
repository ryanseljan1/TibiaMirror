using System.Windows;
using System.Windows.Controls;
using TibiaMirror.Timers;

namespace TibiaMirror.Views;

public partial class AudioTimersView : UserControl
{
    AppHost? _host;
    AudioTimersViewModel? Vm => DataContext as AudioTimersViewModel;

    public AudioTimersView() { InitializeComponent(); Loaded += (_, _) => { if (_host?.TimersVm is { } vm) DataContext = vm; }; }

    /// <summary>Called by MainWindow; the view model is bound on Loaded because AppHost.Start() runs after the window is built.</summary>
    public void Attach(AppHost host) => _host = host;

    static AudioTimerCardViewModel? Card(object sender) => (sender as FrameworkElement)?.DataContext as AudioTimerCardViewModel;

    void Add_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is not { } vm) return;
        var dlg = new TimerEditDialog(null, vm.Host.Hotkeys, vm.Service) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result is { } m) vm.AddTimer(m);
    }

    void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is not { } vm || Card(sender) is not { } card) return;
        var dlg = new TimerEditDialog(card.Model, vm.Host.Hotkeys, vm.Service) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result is { } m) vm.UpdateTimer(card.Model, m);
    }

    void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is not { } vm || Card(sender) is not { } card) return;
        if (MessageBox.Show(Window.GetWindow(this), $"Delete timer \"{card.Name}\"?", "Delete timer",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            vm.DeleteTimer(card.Model);
    }

    void Test_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is { } vm && Card(sender) is { } card) vm.Test(card.Model);
    }
}
