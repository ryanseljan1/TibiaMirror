using TibiaMirror.Geometry; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Geometry;

public class ZoomMathTests
{
    static readonly PixelRect Crop = new(10, 10, 200, 100);

    [Theory]
    [InlineData(1.0, 1, 1.1)]
    [InlineData(1.0, -1, 0.9)]
    [InlineData(1.0, 3, 1.3)]
    [InlineData(0.5, -1, 0.5)]    // clamped at the minimum
    [InlineData(4.0, 1, 4.0)]     // clamped at the maximum
    [InlineData(3.95, 1, 4.0)]
    [InlineData(1.04, 1, 1.1)]    // snaps back onto the 0.1 grid
    [InlineData(1.0, 100, 4.0)]
    [InlineData(1.0, -100, 0.5)]
    public void NextScale_steps_and_clamps(double scale, int notches, double expected) =>
        Assert.Equal(expected, ZoomMath.NextScale(scale, notches), 6);

    [Fact] public void Scale_is_relative_to_crop_width() =>
        Assert.Equal(1.5, ZoomMath.ScaleOf(new PixelRect(0, 0, 300, 150), Crop), 6);

    [Fact] public void Zoom_in_anchored_top_left_keeps_origin_and_aspect()
    {
        var r = ZoomMath.Zoom(new PixelRect(500, 400, 200, 100), Crop, 1);
        Assert.Equal(new PixelRect(500, 400, 220, 110), r);
    }

    [Fact] public void Zoom_out_below_half_is_clamped()
    {
        var r = ZoomMath.Zoom(new PixelRect(0, 0, 100, 50), Crop, -5);
        Assert.Equal(new PixelRect(0, 0, 100, 50), r);
    }

    [Fact] public void Zoom_above_four_is_clamped()
    {
        var r = ZoomMath.Zoom(new PixelRect(0, 0, 800, 400), Crop, 5);
        Assert.Equal(new PixelRect(0, 0, 800, 400), r);
    }

    [Fact] public void Zoom_with_anchor_keeps_the_anchor_point_fixed()
    {
        // anchor at the centre: the centre stays put as the window grows
        var cur = new PixelRect(100, 100, 200, 100);
        var r = ZoomMath.Zoom(cur, Crop, 1, 0.5, 0.5);
        Assert.Equal(new PixelRect(90, 95, 220, 110), r);
    }

    [Fact] public void Zoom_from_a_resized_window_snaps_to_grid()
    {
        // 1.04x -> one notch up -> 1.1x
        var r = ZoomMath.Zoom(new PixelRect(0, 0, 208, 104), Crop, 1);
        Assert.Equal(new PixelRect(0, 0, 220, 110), r);
    }

    [Fact] public void Empty_crop_leaves_window_unchanged()
    {
        var cur = new PixelRect(5, 5, 100, 50);
        Assert.Equal(cur, ZoomMath.Zoom(cur, default, 1));
    }

    [Fact] public void Result_never_goes_below_min_size()
    {
        var tiny = new PixelRect(0, 0, 8, 8);
        var r = ZoomMath.Zoom(new PixelRect(0, 0, 8, 8), tiny, -1);
        Assert.True(r.Width >= CropMath.MinSize && r.Height >= CropMath.MinSize);
    }

    [Theory]
    [InlineData(0, 120, 1, 0)]
    [InlineData(0, -120, -1, 0)]
    [InlineData(0, 240, 2, 0)]
    [InlineData(0, 30, 0, 30)]      // high-resolution wheels report fractions of a notch: they accumulate
    [InlineData(90, 30, 1, 0)]
    [InlineData(90, 60, 1, 30)]
    [InlineData(-90, -60, -1, -30)]
    [InlineData(90, -30, 0, -30)]   // direction change discards the opposite carry
    [InlineData(0, 0, 0, 0)]
    public void Accumulate_emits_whole_notches_and_keeps_remainder(int carry, int delta, int notches, int rest) =>
        Assert.Equal((notches, rest), ZoomMath.Accumulate(carry, delta));

    [Fact] public void WheelAccumulator_needs_four_quarter_notches_for_one_step()
    {
        var w = new WheelAccumulator();
        Assert.Equal(0, w.Add(30)); Assert.Equal(0, w.Add(30)); Assert.Equal(0, w.Add(30));
        Assert.Equal(1, w.Add(30));
        Assert.Equal(0, w.Add(30));
    }
}
