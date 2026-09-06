using System.ComponentModel;
using System.Globalization;

namespace OverjoyedVersion3;

/// <summary>
/// Lets ColorGrid.StartColors/EndColors be set from XAML as a plain comma-separated attribute,
/// e.g. StartColors="#F3F4F7,#FFFFFF,#F8F1F7".
/// </summary>
public class HexColorListTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text
            ? text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : Array.Empty<string>();
}

public partial class ColorGrid : ContentView
{
    private static readonly string[] DefaultStartColors = 
        ["#FF6347", "#FFE95C", "#33FF44", "#73A054", "#4BFBEA", "#5C78AD", "#926BDB", "#EC5BCA", "#FFFFFF", "#DFCBA5"];
    private static readonly string[] DefaultEndColors = 
        ["#F52500", "#FFDE0A", "#00E013", "#4D6B48", "#05F0D8", "#3E5371", "#6430C5", "#DB1AAE", "#000000", "#CAA868"];
    private const int DefaultStepNumber = 5;

    public static readonly BindableProperty StartColorsProperty = BindableProperty.Create(
        nameof(StartColors), typeof(string[]), typeof(ColorGrid),
        defaultValueCreator: _ => DefaultStartColors, propertyChanged: OnPaletteSourceChanged);

    public static readonly BindableProperty EndColorsProperty = BindableProperty.Create(
        nameof(EndColors), typeof(string[]), typeof(ColorGrid),
        defaultValueCreator: _ => DefaultEndColors, propertyChanged: OnPaletteSourceChanged);

    public static readonly BindableProperty StepNumberProperty = BindableProperty.Create(
        nameof(StepNumber), typeof(int), typeof(ColorGrid),
        DefaultStepNumber, propertyChanged: OnPaletteSourceChanged);

    [TypeConverter(typeof(HexColorListTypeConverter))]
    public string[] StartColors
    {
        get => (string[])GetValue(StartColorsProperty);
        set => SetValue(StartColorsProperty, value);
    }

    [TypeConverter(typeof(HexColorListTypeConverter))]
    public string[] EndColors
    {
        get => (string[])GetValue(EndColorsProperty);
        set => SetValue(EndColorsProperty, value);
    }

    public int StepNumber
    {
        get => (int)GetValue(StepNumberProperty);
        set => SetValue(StepNumberProperty, value);
    }

    public string[] Palette { get; private set; } = [];
    public event EventHandler<string>? ColorSelected;
    private string? _selectedColor;
    private readonly Dictionary<string, (Border Cell, Label Check)> _cells = [];

    public ColorGrid()
    {
        InitializeComponent();
        BuildPalette();
        BuildCells();
    }

    private static void OnPaletteSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var grid = (ColorGrid)bindable;
        grid.BuildPalette();
        grid.BuildCells();
    }

    private void BuildPalette()
    {
        if (StartColors.Length != EndColors.Length)
        {
            throw new ArgumentException(
                $"StartColors ({StartColors.Length}) and EndColors ({EndColors.Length}) must have the same length.");
        }

        var palette = new string[StartColors.Length * StepNumber];
        var currentPaletteIndex = 0;

        for (var i = 0; i < StartColors.Length; i++)
        {
            var start = Color.FromArgb(StartColors[i]);
            var end = Color.FromArgb(EndColors[i]);

            for (var j = 0; j < StepNumber; j++)
            {
                var t = (float)j / (StepNumber - 1);
                palette[currentPaletteIndex] = ToHex(Color.FromRgba(
                    start.Red + (end.Red - start.Red) * t,
                    start.Green + (end.Green - start.Green) * t,
                    start.Blue + (end.Blue - start.Blue) * t,
                    1f));
                currentPaletteIndex++;
            }
        }

        Palette = palette;
    }

    private void BuildCells()
    {
        _cells.Clear();
        _selectedColor = null;
        CellContainer.Children.Clear();

        foreach (var hex in Palette)
        {
            var check = new Label
            {
                Text = "✓",
                TextColor = Microsoft.Maui.Graphics.Colors.White,
                BackgroundColor = Color.FromArgb("#66000000"),
                FontSize = 9,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 2, 2, 0),
                Padding = new Thickness(1, 0),
                IsVisible = false,
            };

            var cell = new Border
            {
                BackgroundColor = Color.FromArgb(hex),
                StrokeThickness = 2,
                Stroke = new SolidColorBrush(Microsoft.Maui.Graphics.Colors.Transparent),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(6) },
                WidthRequest = 48,
                HeightRequest = 48,
                Margin = new Thickness(0, 0, 4, 4),
                Content = new Grid { Children = { check } },
            };

            var capturedHex = hex;
            cell.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => SelectColor(capturedHex))
            });

            _cells[hex] = (cell, check);
            CellContainer.Add(cell);
        }
    }

    /// <summary>
    /// Highlights the given swatch as selected without raising ColorSelected. Used to sync the
    /// grid to a persisted setting when the containing view is constructed. No-op if the hex
    /// isn't one of this grid's swatches (e.g. a custom color was chosen instead).
    /// </summary>
    public void SetSelected(string hex)
    {
        if (_cells.ContainsKey(hex))
        {
            SelectColor(hex, notify: false);
        }
    }

    private void SelectColor(string hex, bool notify = true)
    {
        if (_selectedColor != null && _cells.TryGetValue(_selectedColor, out var prev))
        {
            prev.Cell.Stroke = new SolidColorBrush(Microsoft.Maui.Graphics.Colors.Transparent);
            prev.Check.IsVisible = false;
        }

        _selectedColor = hex;
        var current = _cells[hex];
        current.Cell.Stroke = new SolidColorBrush(Microsoft.Maui.Graphics.Colors.White);
        current.Check.IsVisible = true;

        if (notify)
        {
            ColorSelected?.Invoke(this, hex);
        }
    }

    private static string ToHex(Color color) =>
        $"#{(byte)Math.Round(color.Red * 255):X2}{(byte)Math.Round(color.Green * 255):X2}{(byte)Math.Round(color.Blue * 255):X2}";
}
