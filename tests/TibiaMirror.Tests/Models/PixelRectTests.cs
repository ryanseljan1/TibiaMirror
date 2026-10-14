using TibiaMirror.Models;
namespace TibiaMirror.Tests.Models;

public class PixelRectTests
{
    [Fact] public void RightAndBottom() { var r = new PixelRect(10, 20, 30, 40); Assert.Equal(40, r.Right); Assert.Equal(60, r.Bottom); }
    [Fact] public void Offset_moves() => Assert.Equal(new PixelRect(15, 17, 30, 40), new PixelRect(10, 20, 30, 40).Offset(5, -3));
    [Fact] public void Intersect_overlap() => Assert.Equal(new PixelRect(5, 5, 5, 5), new PixelRect(0, 0, 10, 10).Intersect(new PixelRect(5, 5, 10, 10)));
    [Fact] public void Intersect_disjoint_is_empty() => Assert.True(new PixelRect(0, 0, 10, 10).Intersect(new PixelRect(20, 20, 5, 5)).IsEmpty);
    [Fact] public void FromEdges_normalizes_reversed_drag() => Assert.Equal(new PixelRect(5, 5, 10, 10), PixelRect.FromCorners(15, 15, 5, 5));
}
