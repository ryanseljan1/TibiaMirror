using TibiaMirror.Geometry; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Geometry;

public class CropMathTests
{
    static readonly PixelRect Client = new(108, 131, 1600, 900);   // client area on screen
    static readonly PixelRect Window = new(100, 100, 1616, 939);   // window incl. borders/title

    [Fact] public void Selection_inside_client_becomes_client_relative() =>
        Assert.Equal(new PixelRect(92, 69, 300, 200), CropMath.FromSelection(new PixelRect(200, 200, 300, 200), Client));

    [Fact] public void Selection_partly_outside_is_clamped() =>
        Assert.Equal(new PixelRect(0, 0, 92, 69), CropMath.FromSelection(new PixelRect(0, 0, 200, 200), Client));

    [Fact] public void Tiny_selection_is_rejected() =>
        Assert.True(CropMath.FromSelection(new PixelRect(200, 200, 7, 50), Client).IsEmpty);

    [Fact] public void Thumbnail_source_adds_nonclient_offset() =>
        Assert.Equal(new PixelRect(100, 100, 300, 200), CropMath.ToThumbnailSource(new PixelRect(92, 69, 300, 200), Window, Client));

    [Fact] public void Thumbnail_source_unchanged_when_window_moves()   // Review Focus #1
    {
        var moved = 500;
        var src = CropMath.ToThumbnailSource(new PixelRect(92, 69, 300, 200), Window.Offset(moved, moved), Client.Offset(moved, moved));
        Assert.Equal(new PixelRect(100, 100, 300, 200), src);
    }

    [Fact] public void Crop_beyond_resized_client_is_clipped()        // Review Focus #1 (Tibia made smaller)
    {
        var smallClient = new PixelRect(108, 131, 200, 150);
        var smallWindow = new PixelRect(100, 100, 216, 189);
        Assert.Equal(new PixelRect(100, 100, 108, 81), CropMath.ToThumbnailSource(new PixelRect(92, 69, 300, 200), smallWindow, smallClient));
    }

    [Fact] public void Resize_keeps_aspect_using_larger_change() =>
        Assert.Equal(new PixelRect(10, 10, 400, 200), CropMath.ResizeKeepingAspect(new PixelRect(10, 10, 200, 100), 400, 120, 2.0));

    [Fact] public void Resize_never_below_minimum() =>
        Assert.Equal(new PixelRect(10, 10, 16, 8), CropMath.ResizeKeepingAspect(new PixelRect(10, 10, 200, 100), 2, 1, 2.0));
}
