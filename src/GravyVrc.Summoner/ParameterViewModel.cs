using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GravyVrc.Summoner.Core;

namespace GravyVrc.Summoner;

public class ParameterViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private string _name = "Gv/Summoner/Value";
    private int _intValue;
    private bool _boolValue;
    private float _floatValue;
    private ParameterType _type;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public int IntValue
    {
        get => _intValue;
        set => SetField(ref _intValue, value);
    }

    public bool BoolValue
    {
        get => _boolValue;
        set => SetField(ref _boolValue, value);
    }

    public float FloatValue
    {
        get => _floatValue;
        set => SetField(ref _floatValue, value);
    }

    public ParameterType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

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

    public object Value => Type switch
    {
        ParameterType.Int => IntValue,
        ParameterType.Bool => BoolValue,
        ParameterType.Float => FloatValue,
        _ => throw new ArgumentException()
    };
}

public enum ParameterType
{
    Int,
    Bool,
    Float
}

internal class ParameterListViewModel : INotifyPropertyChanged
{
    private bool _canWrite;

    public bool CanWrite
    {
        get => _canWrite;
        set => SetField(ref _canWrite, value);
    }

    public bool IsValid => Collection.Any() && Collection.All(vm => vm.IsValid);

    public ObservableCollection<ParameterViewModel> Collection { get; } = new();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}