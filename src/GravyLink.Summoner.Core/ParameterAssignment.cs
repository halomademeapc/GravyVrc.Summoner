using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GravyVrc.Summoner.Core;

public class ParameterAssignment<T> : ParameterAssignmentBase, INotifyPropertyChanged where T : struct, IComparable, IComparable<T>, IEquatable<T>
{
    private string _name = "GravyVrc.Summoner:Value";
    private T _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string Name
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

    public T Value
    {
        get => _value;
        set
        {
            if (!value.Equals(_value))
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }

    public override object ObjectValue => Value;

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}

public abstract class ParameterAssignmentBase
{
    public abstract string Name { get; set; }
    public abstract object ObjectValue { get; }
}