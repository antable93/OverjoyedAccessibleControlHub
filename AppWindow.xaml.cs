namespace OverjoyedVersion3;

public partial class AppWindow : Window
{
    private bool _settingsOpened = false;
    private bool _transparentBackground;
    private bool _alwaysOnTop;
    private ImageSource? _settingsIcon; 
    private ImageSource? _settingsIconFilled; 

	public AppWindow()
	{
		InitializeComponent();
        _ = InitializeAsync();
        IconManager.IconsRecolored += (_, _) => ReloadIcons();
        if (Page is MainPage mainPage)
        {
            mainPage.Appeared += (_, _) => OpenSettingsOnStartup();
        }
	}
    private async Task InitializeAsync()
    {
        await IconManager.Initialize();
        ReloadIcons();
    }
    private void ReloadIcons()
    {
        _settingsIcon = IconManager.GetIcon("gear")?.Source;
        _settingsIconFilled = IconManager.GetIcon("gear-filled")?.Source;
        SettingsButton.Source = _settingsOpened ? _settingsIconFilled : _settingsIcon;
    }

    // Opens the Controller Editor (the default settings submenu) once, the first time MainPage loads.
    private void OpenSettingsOnStartup()
    {
        if (_settingsOpened) return;
        _settingsOpened = true;
        (Page as MainPage)?.OpenSettings();
        ReloadIcons();
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        if (!_settingsOpened)
        {
            (Page as MainPage)?.OpenSettings();
            SettingsButton.Source = _settingsIconFilled;
            _settingsOpened = true;
        }
        else
        {
            (Page as MainPage)?.CloseSettings();
            SettingsButton.Source = _settingsIcon;
            _settingsOpened = false;
        }
    }

    private void OnBackgroundToggleClicked(object sender, EventArgs e)
    {
        _transparentBackground = !_transparentBackground;
        if (Page is not null)
        {
            Page.BackgroundColor = _transparentBackground ? Colors.Transparent : Colors.Black;
        }
    }

#if WINDOWS
    private void OnAlwaysOnTopClicked(object sender, EventArgs e)
    {
        _alwaysOnTop = !_alwaysOnTop;
        NativeMethods.SetAlwaysOnTop(_alwaysOnTop);
        AlwaysOnTopButton.BackgroundColor = _alwaysOnTop ? Colors.SlateBlue : null;
    }
#else
    private void OnAlwaysOnTopClicked(object sender, EventArgs e) { }
#endif
}