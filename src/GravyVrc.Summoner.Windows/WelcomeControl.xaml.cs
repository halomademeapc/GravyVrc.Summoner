using GravyVrc.Summoner.Windows.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GravyVrc.Summoner.Windows;

public partial class WelcomeControl : UserControl
{
    public const string DisableOnboardingPreferenceKey = "disable_onboarding";

    public WelcomeControl()
    {
        InitializeComponent();
    }

    private void OnDisableChecked(object sender, RoutedEventArgs routedEventArgs)
    {
        var checkbox = sender as CheckBox;
        if (checkbox != null) 
            PreferenceHelper.Set(DisableOnboardingPreferenceKey, checkbox.IsChecked);
    }
}