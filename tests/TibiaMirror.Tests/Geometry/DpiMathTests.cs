using TibiaMirror.Geometry;
namespace TibiaMirror.Tests.Geometry;
public class DpiMathTests
{
    [Theory][InlineData(96u, 1.0)][InlineData(144u, 1.5)][InlineData(120u, 1.25)]
    public void Scale_from_dpi(uint dpi, double expected) => Assert.Equal(expected, DpiMath.Scale(dpi), 3);
    [Fact] public void Roundtrip_150_percent() { Assert.Equal(300, DpiMath.ToPhysical(200, 1.5)); Assert.Equal(200, DpiMath.ToDip(300, 1.5), 3); }
    [Fact] public void ToPhysical_rounds() => Assert.Equal(13, DpiMath.ToPhysical(10.2, 1.25));
}
