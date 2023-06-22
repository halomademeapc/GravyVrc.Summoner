using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT;
using Window = Microsoft.UI.Xaml.Window;

namespace GravyVrc.Summoner.Windows.Helpers;

public static class WindowHelpers
{
    public static void TryMicaOrAcrylic(this Window window)
    {
        var dispatcherQueueHelper = new WindowsSystemDispatcherQueueHelper(); // in Platforms.Windows folder
        dispatcherQueueHelper.EnsureWindowsSystemDispatcherQueueController();

        // Hooking up the policy object
        var configurationSource = new SystemBackdropConfiguration();
        configurationSource.IsInputActive = true;

        switch (((FrameworkElement)window.Content).ActualTheme)
        {
            case ElementTheme.Dark:
                configurationSource.Theme = SystemBackdropTheme.Dark;
                break;
            case ElementTheme.Light:
                configurationSource.Theme = SystemBackdropTheme.Light;
                break;
            case ElementTheme.Default:
                configurationSource.Theme = SystemBackdropTheme.Default;
                break;
        }

        // Let's try Mica first
        if (MicaController.IsSupported())
        {
            var micaController = new MicaController();
            micaController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
            micaController.SetSystemBackdropConfiguration(configurationSource);

            window.Activated += (object sender, WindowActivatedEventArgs args) =>
            {
                if (args.WindowActivationState is WindowActivationState.CodeActivated or WindowActivationState.PointerActivated)
                {
                    // Handle situation where a window is activated and placed on top of other active windows.
                    if (micaController == null)
                    {
                        micaController = new MicaController();
                        micaController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>()); // Requires 'using WinRT;'
                        micaController.SetSystemBackdropConfiguration(configurationSource);
                    }
                    if (configurationSource != null)
                        configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
                }
            };

            window.Closed += (object sender, WindowEventArgs args) =>
            {
                if (micaController != null)
                {
                    micaController.Dispose();
                    micaController = null;
                }
                configurationSource = null;
            };
        }
        // If no Mica, maybe we can use Acrylic instead
        else if (DesktopAcrylicController.IsSupported())
        {
            var acrylicController = new DesktopAcrylicController();
            acrylicController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
            acrylicController.SetSystemBackdropConfiguration(configurationSource);

            window.Activated += (object sender, WindowActivatedEventArgs args) =>
            {
                if (args.WindowActivationState is WindowActivationState.CodeActivated or WindowActivationState.PointerActivated)
                {
                    // Handle situation where a window is activated and placed on top of other active windows.
                    if (acrylicController == null)
                    {
                        acrylicController = new DesktopAcrylicController();
                        acrylicController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>()); // Requires 'using WinRT;'
                        acrylicController.SetSystemBackdropConfiguration(configurationSource);
                    }
                }
                if (configurationSource != null)
                    configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            };

            window.Closed += (object sender, WindowEventArgs args) =>
            {
                if (acrylicController != null)
                {
                    acrylicController.Dispose();
                    acrylicController = null;
                }
                configurationSource = null;
            };
        }
    }

    public static void Resize(this Window window, int width, int height)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(width, height));
    }
}
