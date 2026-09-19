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
                        // Force WS_EX_APPWINDOW so the taskbar icon always shows, even for this
                        // unpackaged, no-activate, always-on-top window.
                        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
                            (IntPtr)(exStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_APPWINDOW));

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
