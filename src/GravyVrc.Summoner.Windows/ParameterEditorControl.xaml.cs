using System;
using System.Linq;
using GravyVrc.Summoner.Core;
using GravyVrc.Summoner.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace GravyVrc.Summoner.Windows;

public partial class ParameterEditorControl : UserControl
{
    private ContentDialog _dialogRef;

    public ParameterEditorControl()
    {
        InitializeComponent();
        ParameterTypePicker.ItemsSource = Enum.GetValues(typeof(ParameterType)).Cast<ParameterType>();
    }

    public void Initialize(ContentDialog dialogRef)
    {
        if (DataContext is not ParameterViewModel context)
            return;
        ViewModel = context;
        ViewModel.PropertyChanged += (_, _) => OnViewModelChange();
        OnViewModelChange();
        _dialogRef = dialogRef;
    }

    private void OnViewModelChange()
    {
        FloatInputLayout.Visibility = ViewModel.Type == ParameterType.Float ? Visibility.Visible : Visibility.Collapsed;
        IntInputLayout.Visibility = ViewModel.Type == ParameterType.Int ? Visibility.Visible : Visibility.Collapsed;
        BoolInputLayout.Visibility = ViewModel.Type == ParameterType.Bool ? Visibility.Visible : Visibility.Collapsed;
        // if (ViewModel.Type == ParameterType.Bool)
        // {
        //     TrueRadio.IsChecked = ViewModel.BoolValue;
        //     FalseRadio.IsChecked = !ViewModel.BoolValue;
        // }

        SaveButton.IsEnabled = ViewModel.IsValid;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs routedEventArgs)
    {
        if (!ViewModel.IsValid)
            return;
        _dialogRef?.Hide();
    }
}