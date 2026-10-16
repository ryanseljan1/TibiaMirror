using System.Windows;
using System.Windows.Controls;
using TibiaMirror.Localization;
using TibiaMirror.Logging;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

/// <summary>Right-click menu of an unlocked mirror window. Every item calls the same <see cref="MirrorService"/> method as the Mirrors page card.</summary>
public static class MirrorContextMenu
{
    /// <summary>Built fresh on every open so checks, Re-crop availability and glow state are current.</summary>
    public static ContextMenu Build(RegionModel region, MirrorService service)
    {
        var id = region.Id;
        var menu = new ContextMenu();

        var recrop = new MenuItem { Header = Loc.Get("Mirrors_Recrop"), IsEnabled = service.CanCapture };
        if (!service.CanCapture)
        {
            recrop.ToolTip = MirrorsViewModel.OpenTibiaTip;
            ToolTipService.SetShowOnDisabled(recrop, true);
        }
        recrop.Click += async (_, _) =>
        {
            try { await service.RecropAsync(id); }
            catch (Exception e) { Log.Error("Re-crop from the mirror menu failed", e); }
        };
        menu.Items.Add(recrop);

        var glow = new MenuItem { Header = Loc.Get("Mirrors_Glow") };
        foreach (var item in GlowMenu.Items(region.Glow, g => service.SetGlow(id, g))) glow.Items.Add(item);
        menu.Items.Add(glow);

        menu.Items.Add(new Separator());
        var lockItem = new MenuItem { Header = Loc.Get("Mirrors_Lock") };
        lockItem.Click += (_, _) => service.SetLocked(id, true);
        menu.Items.Add(lockItem);
        var hide = new MenuItem { Header = Loc.Get("Mirrors_Hide") };
        hide.Click += (_, _) => service.SetVisible(id, false);
        menu.Items.Add(hide);

        menu.Items.Add(new Separator());
        var delete = new MenuItem { Header = Loc.Get("Mirrors_Delete") };
        // deferred: deleting closes this very window, so let the menu finish closing first
        delete.Click += (_, _) => Application.Current.Dispatcher.BeginInvoke(() => service.RequestDelete(id));
        menu.Items.Add(delete);
        return menu;
    }
}
