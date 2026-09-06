using System.Diagnostics;

namespace OverjoyedVersion3;

public class Icon
{
    public Microsoft.Maui.Graphics.IImage? Visual { get; private set; }
    public ImageSource? Source { get; private set; } 

    private static ISvgRasterizer _rasterizer = new SkiaSharpSvgRasterizer();
    
    public static implicit operator ImageSource?(Icon? icon) => icon?.Source;

    private Icon() { }

    public static async Task<Icon> GenerateAsync(string svgSource, int width = 64, int height = 64,
        int r = 255, int g = 255, int b = 255, int a = 255)
    {
        var icon = new Icon();

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(svgSource);
            icon.Visual = _rasterizer.ResterizeSvg(stream, height, width, r, g, b, a);

            if (icon.Visual != null)
            {
                icon.Source = ImageSource.FromStream(() => icon.Visual.AsStream());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load icon '{svgSource}': {ex.Message}");
        }

        return icon;
    }
}
