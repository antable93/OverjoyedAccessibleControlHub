namespace OverjoyedVersion3;

public interface ISvgRasterizer
{
    public Microsoft.Maui.Graphics.IImage? ResterizeSvg(Stream svgStream, int height, int width,
        int r, int g, int b, int a);
}
