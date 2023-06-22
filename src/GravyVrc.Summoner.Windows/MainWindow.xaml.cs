using GravyVrc.Summoner.Windows.Helpers;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GravyVrc.Summoner.Windows;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainFrame.Navigate(typeof(ParameterListPage));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        try
        {
            this.Resize(400, 800);
        }
        catch { }
    }
}