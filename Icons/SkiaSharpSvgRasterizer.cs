using SkiaSharp;
using Microsoft.Maui.Graphics.Platform;

namespace OverjoyedVersion3;

public class SkiaSharpSvgRasterizer : ISvgRasterizer
{
    public Microsoft.Maui.Graphics.IImage? ResterizeSvg(Stream svgStream, int height, int width,
        int r, int g, int b, int a)
    {
        var svg = new Svg.Skia.SKSvg();
        svg.Load(svgStream);

        if (svg.Picture is null) return null;

        var info = new SKImageInfo(width, height);

        using var bitmap = new SKBitmap(info);
        using var svgCanvas = new SKCanvas(bitmap);
        svgCanvas.Clear(SKColors.Transparent);

        var bounds = svg.Picture.CullRect;
        svgCanvas.Scale(width / bounds.Width, height / bounds.Height);
        svgCanvas.DrawPicture(svg.Picture);

        using var tinted = new SKBitmap(info);
        using var tintCanvas = new SKCanvas(tinted);
        tintCanvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateBlendMode(ConvertColor(r, g, b, a), SKBlendMode.SrcIn)
        };
        tintCanvas.DrawBitmap(bitmap, 0, 0, paint);

        using var image = SKImage.FromBitmap(tinted);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return PlatformImage.FromStream(data.AsStream());
    }

    private SKColor ConvertColor(int r, int g, int b, int a)
    {
        Color color = Color.FromRgba(r, g, b, a);
        float h, s, l;
        color.ToHsl(out h, out s, out l);

        return SKColor.FromHsl(h, s * 100f, l * 100f).WithAlpha((byte)a);
    }
}
