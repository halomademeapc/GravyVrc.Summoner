using Windows.Storage;

namespace GravyVrc.Summoner.Windows.Helpers;

public static class PreferenceHelper
{
    public static void Set<T>(string key, T value)
    {
        var settings = ApplicationData.Current.LocalSettings;
        settings.Values[key] = value;
    }

    public static T Get<T>(string key, T defaultValue = default)
    {
        var settings = ApplicationData.Current.LocalSettings;
        return settings.Values.TryGetValue(key, out var val) && val is T casted ? casted : defaultValue;
    }
}