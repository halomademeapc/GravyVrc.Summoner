using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GravyVrc.Summoner;

public partial class ParameterEditorPage : ContentPage
{
    public ParameterEditorPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is not ParameterViewModel context)
            return;
        ViewModel = context;
        ViewModel.PropertyChanged += (_, _) => OnViewModelChange();
        OnViewModelChange();
    }

    private void OnViewModelChange()
    {
        FloatInputLayout.IsVisible = ViewModel.Type == ParameterType.Float;
        IntInputLayout.IsVisible = ViewModel.Type == ParameterType.Int;
        BoolInputLayout.IsVisible = ViewModel.Type == ParameterType.Bool;
    }

    protected override bool OnBackButtonPressed() => false;

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (ViewModel.IsValid)
            Navigation.PopModalAsync(true);
    }
}