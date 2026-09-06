namespace OverjoyedVersion3;

/// <summary>
/// Singleton for app settings. Persists user-configurable settings via Maui.Storage.Preferences and
/// applies them live to the app via DynamicResource-bound values in Application.Current.Resources.
/// </summary>
public static class SettingsManager
{
    private const string PrimaryColorKey = "PrimaryColor";
    private const string DefaultPrimaryColorHex = "#000000";
    private const string SecondaryColorKey = "SecondaryColor";
    private const string DefaultSecondaryColorHex = "#141414";
    private const string FontColorKey = "FontColor";
    private const string DefaultFontColorHex = "#FFFFFF";

    private const string FontScaleKey = "FontScale";
    private const double DefaultFontScale = 1.0;
    public const double MinFontScale = 1.0;
    public const double MaxFontScale = 2.5;

    private const string FontFamilyKey = "FontFamily";
    public const string DefaultFontFamily = "OpenSansRegular";

    private const double BaseTitleFontSize = 28;
    private const double BaseBodyFontSize = 16;
    private const double BaseSubtextFontSize = 14;
    private const double BaseIconSize = 24;

    public static event EventHandler<Color>? PrimaryColorChanged;
    public static event EventHandler<Color>? SecondaryColorChanged;
    public static event EventHandler<Color>? FontColorChanged;
    public static event EventHandler<double>? FontScaleChanged;
    public static event EventHandler<string>? FontFamilyChanged;

    public static Color PrimaryColor
    {
        get => Color.FromArgb(Preferences.Default.Get(PrimaryColorKey, DefaultPrimaryColorHex));
        set
        {
            Preferences.Default.Set(PrimaryColorKey, ToHex(value));
            Application.Current!.Resources["PrimaryColor"] = value;
            PrimaryColorChanged?.Invoke(null, value);
        }
    }

    public static Color SecondaryColor
    {
        get => Color.FromArgb(Preferences.Default.Get(SecondaryColorKey, DefaultSecondaryColorHex));
        set
        {
            Preferences.Default.Set(SecondaryColorKey, ToHex(value));
            Application.Current!.Resources["SecondaryColor"] = value;
            Application.Current!.Resources["FieldColor"] = FieldColor;
            Application.Current!.Resources["OverlayColor"] = OverlayColor;
            SecondaryColorChanged?.Invoke(null, value);
        }
    }

    public static Color FontColor
    {
        get => Color.FromArgb(Preferences.Default.Get(FontColorKey, DefaultFontColorHex));
        set
        {
            Preferences.Default.Set(FontColorKey, ToHex(value));
            Application.Current!.Resources["FontColor"] = value;
            Application.Current!.Resources["MutedFontColor"] = MutedFontColor;
            Application.Current!.Resources["PlaceholderColor"] = PlaceholderColor;
            FontColorChanged?.Invoke(null, value);
        }
    }

    public static double FontScale
    {
        get => Preferences.Default.Get(FontScaleKey, DefaultFontScale);
        set
        {
            var clamped = Math.Clamp(value, MinFontScale, MaxFontScale);
            Preferences.Default.Set(FontScaleKey, clamped);
            ApplyFontScaleResources();
            FontScaleChanged?.Invoke(null, clamped);
        }
    }

    public static string FontFamily
    {
        get => Preferences.Default.Get(FontFamilyKey, DefaultFontFamily);
        set
        {
            Preferences.Default.Set(FontFamilyKey, value);
            Application.Current!.Resources["AppFontFamily"] = value;
            FontFamilyChanged?.Invoke(null, value);
        }
    }

    public static double TitleFontSize => BaseTitleFontSize * FontScale;
    public static double BodyFontSize => BaseBodyFontSize * FontScale;
    public static double SubtextFontSize => BaseSubtextFontSize * FontScale;
    public static double IconSize => BaseIconSize * FontScale;

    /// <summary>
    /// A much darker shade of SecondaryColor, used for entry/picker fields and buttons that sit
    /// on top of panels so they stay legible against any chosen SecondaryColor.
    /// </summary>
    public static Color FieldColor => Darken(SecondaryColor, 0.5f);

    /// <summary>
    /// The semi-transparent backdrop shown behind the settings menu, derived from SecondaryColor.
    /// </summary>
    public static Color OverlayColor => Darken(SecondaryColor, 0.5f).WithAlpha(0.9f);

    /// <summary>
    /// The highlight/selection color for widgets (dial quadrants, joystick ring) drawn on the live
    /// and editor canvases. A lighter shade of SecondaryColor, matching the widget drawer cards.
    /// </summary>
    public static Color WidgetHighlightColor => Lighten(SecondaryColor, 0.12f);

    /// <summary>
    /// A dimmer shade of FontColor used for secondary/caption labels (field captions, hints).
    /// </summary>
    public static Color MutedFontColor => FontColor.WithAlpha(0.8f);

    /// <summary>
    /// The color used for Entry/Editor placeholder text, derived from FontColor.
    /// </summary>
    public static Color PlaceholderColor => FontColor.WithAlpha(0.4f);

    /// <summary>
    /// Loads persisted settings into Application.Current.Resources. Call once at app startup,
    /// before any page/window is created so DynamicResource bindings resolve immediately.
    /// </summary>
    public static void Initialize()
    {
        Application.Current!.Resources["PrimaryColor"] = PrimaryColor;
        Application.Current!.Resources["SecondaryColor"] = SecondaryColor;
        Application.Current!.Resources["FieldColor"] = FieldColor;
        Application.Current!.Resources["OverlayColor"] = OverlayColor;
        Application.Current!.Resources["FontColor"] = FontColor;
        Application.Current!.Resources["MutedFontColor"] = MutedFontColor;
        Application.Current!.Resources["PlaceholderColor"] = PlaceholderColor;
        Application.Current!.Resources["AppFontFamily"] = FontFamily;
        ApplyFontScaleResources();
    }

    private static void ApplyFontScaleResources()
    {
        Application.Current!.Resources["TitleFontSize"] = TitleFontSize;
        Application.Current!.Resources["BodyFontSize"] = BodyFontSize;
        Application.Current!.Resources["SubtextFontSize"] = SubtextFontSize;
        Application.Current!.Resources["IconSize"] = IconSize;
    }

    /// <summary>
    /// Blends a color toward white by the given amount (0 = unchanged, 1 = white). Used to derive
    /// the lighter card/tile shades that sit on top of a SecondaryColor panel background.
    /// </summary>
    public static Color Lighten(Color color, float amount) => Color.FromRgba(
        color.Red + (1f - color.Red) * amount,
        color.Green + (1f - color.Green) * amount,
        color.Blue + (1f - color.Blue) * amount,
        color.Alpha);

    /// <summary>
    /// Blends a color toward black by the given amount (0 = unchanged, 1 = black).
    /// </summary>
    public static Color Darken(Color color, float amount) => Color.FromRgba(
        color.Red * (1f - amount),
        color.Green * (1f - amount),
        color.Blue * (1f - amount),
        color.Alpha);

    public static string ToHex(Color color) =>
        $"#{(byte)Math.Round(color.Red * 255):X2}{(byte)Math.Round(color.Green * 255):X2}{(byte)Math.Round(color.Blue * 255):X2}";
}
