namespace OverjoyedVersion3;

public partial class ToggleButton : ContentView
{
    // Capsule: 44w, 2px stroke each side, 3px padding each side → 34px inner width
    // Circle: 14px → travel = 34 - 14 = 20px
    private const double Travel = 20;

    public static readonly BindableProperty PrimaryColorProperty =
        BindableProperty.Create(nameof(PrimaryColor), typeof(Color), typeof(ToggleButton), Colors.Gray,
            propertyChanged: (b, _, _) => ((ToggleButton)b).ApplyState());

    public static readonly BindableProperty HighlightColorProperty =
        BindableProperty.Create(nameof(HighlightColor), typeof(Color), typeof(ToggleButton), Colors.White,
            propertyChanged: (b, _, _) => ((ToggleButton)b).ApplyState());

    public static readonly BindableProperty IsToggledProperty =
        BindableProperty.Create(nameof(IsToggled), typeof(bool), typeof(ToggleButton), false,
            propertyChanged: (b, _, n) => ((ToggleButton)b).ApplyState());

    public Color PrimaryColor
    {
        get => (Color)GetValue(PrimaryColorProperty);
        set => SetValue(PrimaryColorProperty, value);
    }

    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public event EventHandler<bool>? Toggled;

    public ToggleButton()
    {
        InitializeComponent();
        ApplyState();
    }

    private void ApplyState()
    {
        if (Capsule == null) return;

        bool isOn = IsToggled;
        var primary = PrimaryColor;

        Capsule.Stroke = new SolidColorBrush(primary);
        Capsule.BackgroundColor = isOn ? primary : Colors.Transparent;
        Circle.Fill = new SolidColorBrush(isOn ? HighlightColor : primary);
        StateLabel.Text = isOn ? "On" : "Off";

        _ = Circle.TranslateTo(isOn ? Travel : 0, 0, 150, Easing.CubicOut);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        IsToggled = !IsToggled;
        Toggled?.Invoke(this, IsToggled);
    }
}
