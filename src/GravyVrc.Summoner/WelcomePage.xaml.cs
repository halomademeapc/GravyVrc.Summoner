namespace GravyVrc.Summoner;

public partial class WelcomePage : ContentPage
{
    public const string DisableOnboardingPreferenceKey = "disable_onboarding";

    public WelcomePage()
    {
        InitializeComponent();
    }

    private void OnDismissClicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
    }

    private void LabelClicked(object sender, TappedEventArgs e)
    {
        DisableOnboardingCheckbox.IsChecked = !DisableOnboardingCheckbox.IsChecked;
    }

    private void OnDisableChecked(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Default.Set(DisableOnboardingPreferenceKey, e.Value);
    }
}