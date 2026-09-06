namespace OverjoyedVersion3;

public partial class SettingsMenu : ContentView
{
    private ControllerEditor? _controllerEditor;
    private AppearanceMenu? _appearanceMenu;
    private WindowOptionsMenu? _windowOptionsMenu;
    private DevicesMenu? _devicesMenu;
    
    private View? _activeSubmenu;

    public SettingsMenu()
    {
        InitializeComponent();
        SettingsManager.SecondaryColorChanged += (_, _) => UpdateButtonHighlights();
    }

    public void Open()
    {
        _controllerEditor ??= new ControllerEditor();
        _appearanceMenu ??= new AppearanceMenu();
        _windowOptionsMenu ??= new WindowOptionsMenu();
        _devicesMenu ??= new DevicesMenu();

        var target = _activeSubmenu ?? _controllerEditor;
        ShowSubmenu(target);
        if (target == _controllerEditor)
        {
            _controllerEditor.Open();
        }
    }

    public void Close()
    {
        if (_activeSubmenu == _controllerEditor)
        {
            _controllerEditor?.Close();
        }
        SubMenuContainer.Content = null;
    }

    private void ShowSubmenu(View submenu)
    {
        _activeSubmenu = submenu;
        SubMenuContainer.Content = submenu;
        UpdateButtonHighlights();
    }

    private void UpdateButtonHighlights()
    {
        EditorButton.BackgroundColor = Colors.Transparent;
        AppearanceButton.BackgroundColor = Colors.Transparent;
        WindowOptionsButton.BackgroundColor = Colors.Transparent;
        DevicesButton.BackgroundColor = Colors.Transparent;

        if (_activeSubmenu == _controllerEditor)
        {
            EditorButton.BackgroundColor = SettingsManager.SecondaryColor;
        }
        else if (_activeSubmenu == _appearanceMenu)
        {
            AppearanceButton.BackgroundColor = SettingsManager.SecondaryColor;
        }
        else if (_activeSubmenu == _windowOptionsMenu)
        {
            WindowOptionsButton.BackgroundColor = SettingsManager.SecondaryColor;
        }
        else if (_activeSubmenu == _devicesMenu)
        {
            DevicesButton.BackgroundColor = SettingsManager.SecondaryColor;
        }
    }

    private void OnEditorButtonClicked(object? sender, EventArgs e)
    {
        if (_controllerEditor == null) return;

        if (_activeSubmenu != _controllerEditor)
        {
            _controllerEditor.Open();
            ShowSubmenu(_controllerEditor);
        }
        
        UpdateButtonHighlights();
    }
    private void OnAppearanceButtonClicked(object? sender, EventArgs e)
    {
        if (_appearanceMenu == null) return;

        if (_activeSubmenu != _appearanceMenu)
        {
            ShowSubmenu(_appearanceMenu);
        }

        UpdateButtonHighlights();
    }
    private void OnWindowOptionsButtonClicked(object? sender, EventArgs e)
    {
        if (_windowOptionsMenu == null) return;

        if (_activeSubmenu != _windowOptionsMenu)
        {
            ShowSubmenu(_windowOptionsMenu);
        }

        UpdateButtonHighlights();
    }
    private void OnDevicesButtonClicked(object? sender, EventArgs e)
    {
        if (_devicesMenu == null) return;

        if (_activeSubmenu != _devicesMenu)
        {
            ShowSubmenu(_devicesMenu);
        }

        UpdateButtonHighlights();
    }


}
