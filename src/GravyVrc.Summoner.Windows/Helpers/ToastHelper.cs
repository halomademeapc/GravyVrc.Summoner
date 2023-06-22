using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace GravyVrc.Summoner.Windows.Helpers;

public static class ToastHelper
{
    public static void Show(string message)
    {
        var builder = new AppNotificationBuilder()
            .SetDuration(AppNotificationDuration.Default)
            .AddText(message);

        var notificationManager = AppNotificationManager.Default;
        notificationManager.Show(builder.BuildNotification());
    }
}