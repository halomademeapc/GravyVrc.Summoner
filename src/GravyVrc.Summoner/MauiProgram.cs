using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
#if WINDOWS10_0_17763_0_OR_GREATER
using GravyVrc.Summoner.Platforms.Windows;
#endif

namespace GravyVrc.Summoner;

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

        builder.ConfigureLifecycleEvents(events =>
        {
#if WINDOWS10_0_17763_0_OR_GREATER
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnWindowCreated(window =>
                {
                    window.TryMicaOrAcrylic();
                    window.Resize(400, 800);
                });
            });
#endif
        });

        return builder.Build();
    }
}