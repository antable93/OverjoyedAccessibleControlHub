using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace OverjoyedVersion3;

internal static class NativeMethods
{
    internal const int GWL_EXSTYLE = -20;
    internal const int WS_EX_NOACTIVATE = 0x08000000;
    internal const int WS_EX_APPWINDOW = 0x00040000;

    internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOACTIVATE = 0x0010;

    internal static IntPtr Hwnd { get; set; }

    internal static void SetNoActivate(bool noActivate)
    {
        var exStyle = GetWindowLongPtr(Hwnd, GWL_EXSTYLE);
        long newStyle = noActivate
            ? exStyle.ToInt64() | WS_EX_NOACTIVATE
            : exStyle.ToInt64() & ~WS_EX_NOACTIVATE;
        SetWindowLongPtr(Hwnd, GWL_EXSTYLE, (IntPtr)newStyle);
    }

    internal static void SetAlwaysOnTop(bool alwaysOnTop)
    {
        SetWindowPos(Hwnd, alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    [DllImport("user32.dll")]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    internal static IReadOnlyList<string> GetSystemFontFamilies()
    {
        using var collection = new InstalledFontCollection();
        return collection.Families
            .Select(f => f.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
