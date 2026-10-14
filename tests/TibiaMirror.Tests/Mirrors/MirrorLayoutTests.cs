using TibiaMirror.Mirrors; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Mirrors;

public class MirrorLayoutTests
{
    static readonly PixelRect Client = new(0, 0, 400, 300);
    static readonly PixelRect Interior = new(2, 2, 200, 100);

    [Fact] public void Fully_visible_crop_fills_interior() =>
        Assert.Equal(Interior, MirrorLayout.DestinationFor(new(100, 100, 100, 50), Client, Interior));

    [Fact] public void Crop_clipped_on_right_half_maps_to_left_half() =>
        Assert.Equal(new PixelRect(2, 2, 100, 100), MirrorLayout.DestinationFor(new(350, 100, 100, 50), Client, Interior));

    [Fact] public void Crop_clipped_on_top_maps_to_bottom_part() =>
        Assert.Equal(new PixelRect(2, 52, 200, 50), MirrorLayout.DestinationFor(new(100, -25, 100, 50), Client, Interior));

    [Fact] public void Crop_outside_client_is_empty() =>
        Assert.True(MirrorLayout.DestinationFor(new(500, 100, 100, 50), Client, Interior).IsEmpty);
}
