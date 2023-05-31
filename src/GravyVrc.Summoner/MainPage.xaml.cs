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
        ViewModel.PropertyChanged += (_, _) => OnViewModelChange();
        _nfcSummoner.ParameterTagScanned += OnNfcTagScanned;
        OnViewModelChange();
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

    private void OnDebugClicked(object sender, EventArgs e)
    {
        //throw new NotImplementedException();
    }
}