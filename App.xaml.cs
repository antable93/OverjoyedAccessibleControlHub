namespace OverjoyedVersion3
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new AppWindow();
            window.Width = 800;
            window.Height = 1000;
            // Transparent background replaced with black
            window.Page?.BackgroundColor = Colors.Black;
#if WINDOWS
            window.Created += (_, _) =>
            {
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    var presenter = nativeWindow.AppWindow.Presenter
                        as Microsoft.UI.Windowing.OverlappedPresenter;
                    if (presenter is not null)
                    {
                        presenter.Maximize();
                        presenter.IsMaximizable = false;
                    }
                }
            };
#endif
            return window;
        }
    }
}
