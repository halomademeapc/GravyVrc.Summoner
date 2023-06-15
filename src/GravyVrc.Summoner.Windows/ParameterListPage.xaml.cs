using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSoft.VRChat.Osc;
using GravyVrc.Summoner.Core;
using GravyVrc.Summoner.Nfc;
using GravyVrc.Summoner.Windows.Helpers;
using GravyVrc.Summoner.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace GravyVrc.Summoner.Windows;

public partial class ParameterListPage : Page
{
    private readonly NfcSummoner _nfcSummoner = new();
    private string _readerName = null;

    private const string TriggerParameterName = "Gv/Summoner/Triggered";
    private const string PresentParameterName = "Gv/Summoner/Present";

    private bool _isFirstOpen = true;

    public ParameterListPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += (_, _) => OnViewModelChange();
        _nfcSummoner.ParameterTagScanned += OnNfcTagScanned;
        _nfcSummoner.ReaderReady += OnReaderReady;
        OnViewModelChange();
        _nfcSummoner.StartListening();
    }

    private void OnReaderReady(ReaderReadyArgs args)
    {
        _readerName = args.ReaderName;
        this.RunOnUiThread(() => { ViewModel.CanWrite = args.IsReady; });
        OscParameter.SendAvatarParameter(PresentParameterName, args.IsReady);
    }

    private void OnNfcTagScanned(IList<ParameterAssignmentBase> parameters)
    {
        SetVrcParameter(parameters);
        this.RunOnUiThread(() =>
        {
            ViewModel.Load(parameters);
            OnViewModelChange();
        });
    }

    private static void SetVrcParameter(IEnumerable<ParameterAssignmentBase> assignments)
    {
        foreach (var assignment in assignments)
            SetVrcParameter(assignment);
        SendTriggerEvent();
    }

    private async Task TryOpenOnboarding()
    {
        if (!_isFirstOpen)
            return;
        if (PreferenceHelper.Get(WelcomeControl.DisableOnboardingPreferenceKey, false))
            return;
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        this.RunOnUiThread(() =>
        {
            var control = new WelcomeControl();
            var dialog = new ContentDialog()
            {
                XamlRoot = Content.XamlRoot,
                Content = control,
                DefaultButton = ContentDialogButton.Close,
                CloseButtonText = "Dismiss"
            };
            dialog.ShowAsync();
        });
    }

    private static async void SendTriggerEvent()
    {
        OscParameter.SendAvatarParameter(TriggerParameterName, true);
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        OscParameter.SendAvatarParameter(TriggerParameterName, false);
    }

    private static void SetVrcParameter(ParameterAssignmentBase assignment)
    {
        switch (assignment)
        {
            case ParameterAssignment<int> intAssignment:
                OscParameter.SendAvatarParameter(intAssignment.Name, intAssignment.Value);
                break;
            case ParameterAssignment<float> floatAssignment:
                OscParameter.SendAvatarParameter(floatAssignment.Name, floatAssignment.Value);
                break;
            case ParameterAssignment<bool> boolAssignment:
                OscParameter.SendAvatarParameter(boolAssignment.Name, boolAssignment.Value);
                break;
        }

        //var toast = Toast.Make($"Set {assignment.Name} to {assignment.ObjectValue}");
        //toast.Show();
    }

    void OnButtonClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        if (ViewModel.IsValid)
            SetVrcParameter(ViewModel.Collection.Select(i => i.GetAssignment()));
    }

    void OnViewModelChange()
    {
        SubmitButton.IsEnabled = ViewModel.IsValid;
        WriteButton.IsEnabled = ViewModel.IsValid && ViewModel.CanWrite;
    }

    private void OnWriteClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        try
        {
            _nfcSummoner.WriteTag(ViewModel.Collection.Select(v => v.GetAssignment()).ToList(), _readerName);
            //var toast = Toast.Make("NFC tag was written successfully!");
            //toast.Show();
        }
        catch
        {
            var dialog = new ContentDialog
            {
                Title = "Write Failure",
                Content = "Unable to write NFC tag and I'm too lazy to put in troubleshooting info just yet.",
                CloseButtonText = "Mega Oof",
                DefaultButton = ContentDialogButton.Close
            };
            dialog.ShowAsync();
        }
    }

    private void OnAddClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        var model = new ParameterViewModel();
        ViewModel.Collection.Add(model);
        OpenEditor(model);
    }

    private async void OpenEditor(ParameterViewModel model)
    {
        var control = new ParameterEditorControl()
        {
            DataContext = model
        };
        var dialog = new ContentDialog()
        {
            XamlRoot = Content.XamlRoot,
            Content = control
        };
        control.Initialize(dialog);
        await dialog.ShowAsync();
    }

    private void OnRemoveClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        var button = sender as ContentControl;
        if (button?.DataContext is not ParameterViewModel entry)
            return;
        ViewModel.Collection.Remove(entry);
        OnViewModelChange();
    }

    private void OnEditClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        var button = sender as ContentControl;
        if (button?.DataContext is ParameterViewModel entry)
            OpenEditor(entry);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        TryOpenOnboarding();
        _isFirstOpen = false;
        OnViewModelChange();
        base.OnNavigatedTo(e);
    }

    private void OnAboutClicked(object sender, EventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Edit Parameter",
            Content = new AboutPage()
        };
        dialog.ShowAsync();
    }
}