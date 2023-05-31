using System.ComponentModel;
using System.Runtime.CompilerServices;
using GravyVrc.Summoner.Core;

namespace GravyVrc.Summoner;

internal class ParameterViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _name = "Gv/Summoner/Value";
    private int _intValue;
    private bool _boolValue;
    private float _floatValue;
    private ParameterType _type;

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

    public int IntValue
    {
        get => _intValue;
        set
        {
            if (value != _intValue)
            {
                _intValue = value;
                OnPropertyChanged();
            }
        }
    }

    public bool BoolValue
    {
        get => _boolValue;
        set
        {
            if (value != _boolValue)
            {
                _boolValue = value;
                OnPropertyChanged();
            }
        }
    }

    public float FloatValue
    {
        get => _floatValue;
        set
        {
            if (value != _floatValue)
            {
                _floatValue = value;
                OnPropertyChanged();
            }
        }
    }

    public ParameterType Type
    {
        get => _type;
        set
        {
            if (value != _type)
            {
                _type = value;
                OnPropertyChanged();
            }
        }
    }

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public bool IsValid => this switch
    {
        { Name: "" } => false,
        { Name: null } => false,
        _ => true
    };

    public ParameterAssignmentBase GetAssignment() => Type switch
    {
        ParameterType.Int => new ParameterAssignment<int> { Name = Name, Value = IntValue },
        ParameterType.Bool => new ParameterAssignment<bool> { Name = Name, Value = BoolValue },
        ParameterType.Float => new ParameterAssignment<float> { Name = Name, Value = FloatValue },
        _ => throw new ArgumentException()
    };
}

public enum ParameterType
{
    Int,
    Bool,
    Float
}