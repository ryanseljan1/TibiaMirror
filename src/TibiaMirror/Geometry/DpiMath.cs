namespace TibiaMirror.Geometry;

public static class DpiMath
{
    public static double Scale(uint dpi) => dpi / 96.0;
    public static int ToPhysical(double dip, double scale) => (int)Math.Round(dip * scale, MidpointRounding.AwayFromZero);
    public static double ToDip(int px, double scale) => px / scale;
}
