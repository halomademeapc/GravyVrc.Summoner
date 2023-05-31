using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GravyVrc.Summoner;

internal class ParameterAssignmentViewModel : INotifyPropertyChanged
{
    private string _name = "GravyVrc.Summoner:Value";
    private int _value;

    public event PropertyChangedEventHandler PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            if (value != _name)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            if (value != _value)
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}
