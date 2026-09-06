namespace OverjoyedVersion3;

public partial class IconButton : ContentView
{
    public static readonly BindableProperty IconSourceProperty =
        BindableProperty.Create(nameof(IconSource), typeof(string), typeof(IconButton), null,
            propertyChanged: (b, _, n) => ((IconButton)b).OnIconSourceChanged((string?)n));

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(IconButton), string.Empty,
            propertyChanged: (b, _, n) => ((IconButton)b).OnTextChanged((string)n));

    public static readonly BindableProperty RoundedProperty =
        BindableProperty.Create(nameof(Rounded), typeof(bool), typeof(IconButton), false,
            propertyChanged: (b, _, n) => ((IconButton)b).OnRoundedChanged((bool)n));

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(IconButton), 14.0,
            propertyChanged: (b, _, n) => ((IconButton)b).ButtonLabel.FontSize = (double)n);

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(double), typeof(IconButton), 24.0,
            propertyChanged: (b, _, n) => ((IconButton)b).OnIconSizeChanged((double)n));

    public string? IconSource
    {
        get => (string?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public new string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool Rounded
    {
        get => (bool)GetValue(RoundedProperty);
        set => SetValue(RoundedProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public event EventHandler? Clicked;

    public IconButton()
    {
        InitializeComponent();
        IconManager.IconsRecolored += (_, _) =>
        {
            if (IconSource != null) ButtonImage.Source = IconManager.GetIcon(IconSource);
        };
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (InnerBorder == null) return;
        if (propertyName == BackgroundColorProperty.PropertyName)
            InnerBorder.BackgroundColor = BackgroundColor;
        else if (propertyName == PaddingProperty.PropertyName)
            InnerBorder.Padding = Padding;
    }

    private void OnIconSourceChanged(string? name)
    {
        ButtonImage.Source = name != null ? IconManager.GetIcon(name) : null;
        ButtonImage.WidthRequest = IconSize;
        ButtonImage.HeightRequest = IconSize;
    }

    private void OnIconSizeChanged(double size)
    {
        ButtonImage.WidthRequest = size;
        ButtonImage.HeightRequest = size;
    }

    private void OnRoundedChanged(bool rounded)
    {
        InnerBorder.StrokeShape = rounded
            ? new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(5) }
            : new Microsoft.Maui.Controls.Shapes.Rectangle();
    }

    private void OnTextChanged(string text)
    {
        ButtonLabel.Text = text;
        ButtonLabel.IsVisible = !string.IsNullOrEmpty(text);
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Clicked?.Invoke(this, e);
}
