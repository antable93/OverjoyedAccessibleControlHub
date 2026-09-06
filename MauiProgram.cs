using Microsoft.Extensions.Logging;

#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
using WinUIEx;
#endif

namespace OverjoyedVersion3
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        NativeMethods.Hwnd = hwnd;
                        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);

                        // Ensure the app does not take focus by default.
                        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
                            (IntPtr)(exStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));

                        // Ensure the ape is always above other apps.
                        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

                        WindowManager.Get(window);
                        window.SystemBackdrop = new TransparentTintBackdrop();

                    });
                });
            });
#endif

            return builder.Build();
        }
    }
}
