using System;
using Microsoft.UI.Xaml;

namespace GravyVrc.Summoner.Windows.Helpers;

public static class ThreadHelper
{
    public static bool RunOnUiThread(this DependencyObject dependencyObject, Action action)
    {
        if (dependencyObject.DispatcherQueue.HasThreadAccess)
        {
            action();
            return false;
        }
        else
        {
            return dependencyObject.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                () => action());
        }
    }
}