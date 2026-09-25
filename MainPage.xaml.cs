namespace OverjoyedVersion3
{
    public partial class MainPage : ContentPage
    {
        private LiveControllerView? _controllerView;
        private SettingsMenu? _settingsMenu;

        public event EventHandler? Appeared;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            SettingsManager.Initialize();
            await IconManager.Initialize();
            await ControllerManager.Initialize();

            if (_settingsMenu == null)
            {
                _settingsMenu = new SettingsMenu { IsVisible = false };
                RootGrid.Add(_settingsMenu);
            }

            DisplayActiveController();

            Appeared?.Invoke(this, EventArgs.Empty);
        }

        public void OpenSettings()
        {
            if (_settingsMenu == null) return;
            _settingsMenu.IsVisible = true;
            _settingsMenu.Open();
        }

        public void CloseSettings()
        {
            if (_settingsMenu == null) return;
            _settingsMenu.Close();
            _settingsMenu.IsVisible = false;
            DisplayActiveController();
        }

        private void DisplayActiveController()
        {
            var controller = ControllerManager.ActiveController;
            if (controller == null) return;

            if (_controllerView == null)
            {
                _controllerView = new LiveControllerView(controller);
                CanvasScrollView.Content = _controllerView;
            }
            else
            {
                _controllerView.DisplayController(controller);
            }

            _controllerView.WidthRequest = Controller.GetLiveControllerWidth();
            _controllerView.HeightRequest = Controller.GetLiveControllerHeight();
        }
    }
}
