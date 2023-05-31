using BuildSoft.VRChat.Osc;

namespace GravyVrc.Summoner;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelChange;
    }

    private static void SetVrcParameterAsync(ParameterAssignmentViewModel assignment)
    {
        OscParameter.SendAvatarParameter(assignment.Name, assignment.Value);
    }

    async void OnButtonClicked(object sender, EventArgs args)
    {
        if (ViewModel.IsValid)
        {
            SetVrcParameterAsync(ViewModel);
        }
    }

    async void OnViewModelChange(object sender, EventArgs args)
    {
        SubmitButton.IsEnabled = ViewModel.IsValid;
    }
}

