using BuildSoft.VRChat.Osc;
using GravyVrc.Summoner.Core;
using GravyVrc.Summoner.Nfc;
using CommunityToolkit.Maui.Alerts;

namespace GravyVrc.Summoner;

public partial class MainPage : ContentPage
{
    private readonly NfcSummoner _nfcSummoner = new();
    private string _readerName = null;

    public MainPage()
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
        ViewModel.CanWrite = args.IsReady;
    }

    private void OnNfcTagScanned(IList<ParameterAssignmentBase> parameters)
    {
        SetVrcParameter(parameters);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ViewModel.Load(parameters);
            OnViewModelChange();
        });
    }
    
    private static void SetVrcParameter(IEnumerable<ParameterAssignmentBase> assignments)
    {
        foreach (var assignment in assignments)
            SetVrcParameter(assignment);
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

        var toast = Toast.Make($"Set {assignment.Name} to {assignment.ObjectValue}");
        toast.Show();
    }

    void OnButtonClicked(object sender, EventArgs args)
    {
        if (ViewModel.IsValid)
            SetVrcParameter(ViewModel.Collection.Select(i => i.GetAssignment()));
    }

    void OnViewModelChange()
    {
        SubmitButton.IsEnabled = ViewModel.IsValid;
        WriteButton.IsEnabled = ViewModel.IsValid && ViewModel.CanWrite;
    }

    private void OnWriteClicked(object sender, EventArgs e)
    {
        try
        {
            _nfcSummoner.WriteTag(ViewModel.Collection.Select(v => v.GetAssignment()).ToList(), _readerName);
            var toast = Toast.Make("NFC tag was written successfully!");
            toast.Show();
        }
        catch
        {
            DisplayAlert("Write Failure",
                "Unable to write NFC tag and I'm too lazy to put in troubleshooting info just yet.", "Mega Oof");
        }
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        var model = new ParameterViewModel();
        ViewModel.Collection.Add(model);
        OpenEditor(model);
    }

    private async void OpenEditor(ParameterViewModel model)
    {
        await Navigation.PushModalAsync(new ParameterEditorPage
        {
            BindingContext = model
        }, true);
    }

    private void OnRemoveClicked(object sender, EventArgs e)
    {
        var button = sender as BindableObject;
        if (button?.BindingContext is not ParameterViewModel entry)
            return;
        ViewModel.Collection.Remove(entry);
        OnViewModelChange();
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        var button = sender as BindableObject;
        if (button?.BindingContext is ParameterViewModel entry)
            OpenEditor(entry);
    }
}