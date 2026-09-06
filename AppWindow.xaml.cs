namespace OverjoyedVersion3;

public partial class AppWindow : Window
{
    private bool _settingsOpened = false;
    private ImageSource? _settingsIcon; 
    private ImageSource? _settingsIconFilled; 

	public AppWindow()
	{
		InitializeComponent();
        _ = InitializeAsync();
        IconManager.IconsRecolored += (_, _) => ReloadIcons();
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
}