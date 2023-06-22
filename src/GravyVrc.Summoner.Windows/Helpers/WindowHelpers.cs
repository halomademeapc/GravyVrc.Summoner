using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Window = Microsoft.UI.Xaml.Window;
using System.Runtime.InteropServices;
using System;

namespace GravyVrc.Summoner.Windows.Helpers;

public static class WindowHelpers
{
    public static void Resize(this Window window, int width, int height)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var dpi = GetDisplayScaleFactor(handle);
        appWindow.Resize(new SizeInt32((int)(width * dpi), (int)(height * dpi)));
    }

    [DllImport("user32.dll")]
    static extern int GetDpiForWindow(IntPtr hWnd);

    public static float GetDisplayScaleFactor(IntPtr windowHandle)
    {
        try
        {
            return GetDpiForWindow(windowHandle) / 96f;
        }
        catch
        {
            // Or fallback to GDI solutions above
            return 1;
        }
    }
}
