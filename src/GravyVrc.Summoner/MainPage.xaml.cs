using BuildSoft.VRChat.Osc;
using GravyVrc.Summoner.Core;
using GravyVrc.Summoner.Nfc;

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

    void OnNfcTagScanned(ParameterAssignmentBase parameter)
    {
        SetVrcParameter(parameter);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ViewModel.Name = parameter.Name;
            switch (parameter)
            {
                case ParameterAssignment<int> intAssignment:
                    ViewModel.Type = ParameterType.Int;
                    ViewModel.IntValue = intAssignment.Value;
                    break;
                case ParameterAssignment<float> floatAssignment:
                    ViewModel.Type = ParameterType.Float;
                    ViewModel.FloatValue = floatAssignment.Value;
                    break;
                case ParameterAssignment<bool> boolAssignment:
                    ViewModel.Type = ParameterType.Bool;
                    ViewModel.BoolValue = boolAssignment.Value;
                    TrueRadio.IsChecked = boolAssignment.Value;
                    FalseRadio.IsChecked = !boolAssignment.Value;
                    break;
            }

            OnViewModelChange();
        });
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
    }

    void OnButtonClicked(object sender, EventArgs args)
    {
        if (ViewModel.IsValid)
        {
            SetVrcParameter(ViewModel.GetAssignment());
        }
    }

    void OnViewModelChange()
    {
        SubmitButton.IsEnabled = ViewModel.IsValid;
        FloatInputLayout.IsVisible = ViewModel.Type == ParameterType.Float;
        IntInputLayout.IsVisible = ViewModel.Type == ParameterType.Int;
        BoolInputLayout.IsVisible = ViewModel.Type == ParameterType.Bool;
    }

    private void OnWriteClicked(object sender, EventArgs e)
    {
        try
        {
            _nfcSummoner.WriteTag(ViewModel.GetAssignment(), _readerName);
            DisplayAlert("Tag Written", "NFC tag was written successfully!", "OK");
        }
        catch
        {
            DisplayAlert("Write Failure",
                "Unable to write NFC tag and I'm too lazy to put in troubleshooting info just yet.", "Mega Oof");
        }
    }
}