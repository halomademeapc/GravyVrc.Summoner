using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GravyVrc.Summoner.Windows;

public partial class AboutPage : Page
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

    private void OnDonateClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        OpenLink("https://paypal.me/halomademeapc");
    }

    private void OnSourceClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        OpenLink("https://github.com/halomademeapc/GravyVrc.Summoner");
    }

    private void OnIssueClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        OpenLink("https://github.com/halomademeapc/GravyVrc.Summoner/issues/new");
    }

    private void OpenLink(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}