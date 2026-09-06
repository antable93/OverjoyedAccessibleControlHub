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
            window.Page?.BackgroundColor = Colors.Transparent;
            return window;
        }
    }
}
