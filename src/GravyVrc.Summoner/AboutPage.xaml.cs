using System.Reflection;

namespace GravyVrc.Summoner;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
        SetVersion();
    }

    private void SetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text = $"v{version}";
    }

    private void OnDonateClicked(object sender, EventArgs e)
    {
        Browser.Default.OpenAsync("https://paypal.me/halomademeapc");
    }

    private void OnSourceClicked(object sender, EventArgs e)
    {
        Browser.Default.OpenAsync("https://github.com/halomademeapc/GravyVrc.Summoner");
    }

    private void OnIssueClicked(object sender, EventArgs e)
    {
        Browser.Default.OpenAsync("https://github.com/halomademeapc/GravyVrc.Summoner/issues/new");
    }
}