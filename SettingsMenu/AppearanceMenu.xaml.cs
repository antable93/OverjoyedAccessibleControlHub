using System.Text.RegularExpressions;

namespace OverjoyedVersion3;

public partial class AppearanceMenu : ContentView
{
	public AppearanceMenu()
	{
		InitializeComponent();

        var currentPrimaryHex = SettingsManager.ToHex(SettingsManager.PrimaryColor);
        CustomPrimaryColorEntry.Text = currentPrimaryHex;
        PrimaryColorGrid.SetSelected(currentPrimaryHex);

        PrimaryColorGrid.ColorSelected += OnPrimaryColorSelected;
        CustomPrimaryColorEntry.Completed += OnCustomPrimaryColorEntryCompleted;

        var currentSecondaryHex = SettingsManager.ToHex(SettingsManager.SecondaryColor);
        CustomSecondaryColorEntry.Text = currentSecondaryHex;
        SecondaryColorGrid.SetSelected(currentSecondaryHex);

        SecondaryColorGrid.ColorSelected += OnSecondaryColorSelected;
        CustomSecondaryColorEntry.Completed += OnCustomSecondaryColorEntryCompleted;

        var currentFontHex = SettingsManager.ToHex(SettingsManager.FontColor);
        CustomFontColor.Text = currentFontHex;
        FontColorGrid.SetSelected(currentFontHex);

        FontColorGrid.ColorSelected += OnFontColorSelected;
        CustomFontColor.Completed += OnCustomFontColorEntryCompleted;

        WindowOpacitySlider.Value = SettingsManager.PrimaryColor.Alpha;
        WindowOpacitySlider.ValueChanged += OnWindowOpacitySliderValueChanged;

        FontScaleSlider.Value = SettingsManager.FontScale;
        FontScaleSlider.ValueChanged += OnFontScaleSliderValueChanged;

        FontFamilyPicker.ItemsSource = new List<string> { SettingsManager.DefaultFontFamily };
        FontFamilyPicker.SelectedIndexChanged += OnFontFamilyPickerSelectedIndexChanged;
        _ = LoadFontFamiliesAsync();
	}

    // GDI+ font enumeration is slow, so run it off the UI thread to avoid stalling first open.
    private async Task LoadFontFamiliesAsync()
    {
        var systemFonts = await Task.Run(FontManager.GetSystemFontFamilies);

        var fontFamilies = new List<string> { SettingsManager.DefaultFontFamily };
        fontFamilies.AddRange(systemFonts);
        FontFamilyPicker.ItemsSource = fontFamilies;
        FontFamilyPicker.SelectedIndex = fontFamilies.IndexOf(SettingsManager.FontFamily);
    }

    private void OnWindowOpacitySliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Color currentColor = SettingsManager.PrimaryColor;
        SettingsManager.PrimaryColor = new Color(currentColor.Red, currentColor.Green, currentColor.Blue, (float)e.NewValue);
    }

    private void OnFontScaleSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        SettingsManager.FontScale = e.NewValue;
    }

    private void OnFontFamilyPickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (FontFamilyPicker.SelectedItem is string fontFamily)
        {
            SettingsManager.FontFamily = fontFamily;
        }
    }

    private void OnPrimaryColorSelected(object? sender, string hex)
    {
        SettingsManager.PrimaryColor = Color.FromArgb(hex);
        CustomPrimaryColorEntry.Text = hex;
    }

    private void OnCustomPrimaryColorEntryCompleted(object? sender, EventArgs e)
    {
        if (!TryParseHexColor(CustomPrimaryColorEntry.Text, out var color)) return;

        SettingsManager.PrimaryColor = color;
        PrimaryColorGrid.SetSelected(SettingsManager.ToHex(color));
    }

    private void OnSecondaryColorSelected(object? sender, string hex)
    {
        SettingsManager.SecondaryColor = Color.FromArgb(hex);
        CustomSecondaryColorEntry.Text = hex;
    }

    private void OnCustomSecondaryColorEntryCompleted(object? sender, EventArgs e)
    {
        if (!TryParseHexColor(CustomSecondaryColorEntry.Text, out var color)) return;

        SettingsManager.SecondaryColor = color;
        SecondaryColorGrid.SetSelected(SettingsManager.ToHex(color));
    }

    private void OnFontColorSelected(object? sender, string hex)
    {
        SettingsManager.FontColor = Color.FromArgb(hex);
        CustomFontColor.Text = hex;
    }

    private void OnCustomFontColorEntryCompleted(object? sender, EventArgs e)
    {
        if (!TryParseHexColor(CustomFontColor.Text, out var color)) return;

        SettingsManager.FontColor = color;
        FontColorGrid.SetSelected(SettingsManager.ToHex(color));
    }

    private static bool TryParseHexColor(string? text, out Color color)
    {
        color = Colors.Black;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var hex = text.StartsWith('#') ? text : $"#{text}";
        if (!Regex.IsMatch(hex, "^#[0-9A-Fa-f]{6}$")) return false;

        color = Color.FromArgb(hex);
        return true;
    }
}
