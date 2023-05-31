using BuildSoft.VRChat.Osc;
using GravyVrc.Summoner.Core;
using GravyVrc.Summoner.Nfc;

namespace GravyVrc.Summoner;

public partial class MainPage : ContentPage
{
    private readonly NfcSummoner _nfcSummoner = new();

    public MainPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelChange;
        _nfcSummoner.ParameterTagScanned += OnNfcTagScanned;
    }

    void OnNfcTagScanned(ParameterAssignmentBase parameter)
    {
        SetVrcParameter(parameter);
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
            SetVrcParameter(ViewModel);
        }
    }

    void OnViewModelChange(object sender, EventArgs args)
    {
        SubmitButton.IsEnabled = ViewModel.IsValid;
    }
}

