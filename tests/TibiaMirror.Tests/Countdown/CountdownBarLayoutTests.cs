using TibiaMirror.Countdown; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Countdown;

public class CountdownBarLayoutTests
{
    static readonly PixelRect M = new(100, 100, 200, 100);
    [Fact] public void Bottom_full() => Assert.Equal(new PixelRect(100, 200, 200, 6), CountdownBarLayout.Compute(M, BarSide.Bottom, BarShape.FullEdge, 6));
    [Fact] public void Top_full() => Assert.Equal(new PixelRect(100, 94, 200, 6), CountdownBarLayout.Compute(M, BarSide.Top, BarShape.FullEdge, 6));
    [Fact] public void Left_full() => Assert.Equal(new PixelRect(94, 100, 6, 100), CountdownBarLayout.Compute(M, BarSide.Left, BarShape.FullEdge, 6));
    [Fact] public void Right_narrow_is_centered_quarter() => Assert.Equal(new PixelRect(300, 137, 6, 25), CountdownBarLayout.Compute(M, BarSide.Right, BarShape.NarrowStrip, 6));
    [Fact] public void Narrow_has_minimum_length() => Assert.Equal(24, CountdownBarLayout.Compute(new PixelRect(0, 0, 40, 40), BarSide.Bottom, BarShape.NarrowStrip, 4).Width);
    [Fact] public void Thickness_is_clamped_low() => Assert.Equal(2, CountdownBarLayout.Compute(M, BarSide.Bottom, BarShape.FullEdge, 0).Height);
    [Fact] public void Thickness_is_clamped_high() => Assert.Equal(40, CountdownBarLayout.Compute(M, BarSide.Bottom, BarShape.FullEdge, 5000).Height);
}
