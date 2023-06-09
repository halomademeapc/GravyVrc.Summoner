using GravyVrc.Summoner.Core;
using MessagePack;

namespace GravyVrc.Summoner.Nfc;

[MessagePackObject]
public class ParameterStorage
{
    [Key(0)]
    public ParameterType Type { get; set; }

    [Key(1)]
    public string? Name { get; set; }

    [Key(2)]
    public int IntValue { get; set; }

    [Key(3)]
    public float FloatValue { get; set; }

    [Key(4)]
    public bool BoolValue { get; set; }

    public ParameterAssignmentBase ToAssignment() => Type switch
    {
        ParameterType.Int => new ParameterAssignment<int>
        {
            Name = Name!,
            Value = IntValue
        },
        ParameterType.Float => new ParameterAssignment<float>
        {
            Name = Name!,
            Value = FloatValue
        },
        ParameterType.Bool => new ParameterAssignment<bool>
        {
            Value = BoolValue,
            Name = Name!
        },
        _ => throw new ArgumentOutOfRangeException()
    };

    public static ParameterStorage FromAssignment(ParameterAssignmentBase assignment) => new ParameterStorage
    {
        Type = assignment switch
        {
            ParameterAssignment<int> => ParameterType.Int,
            ParameterAssignment<float> => ParameterType.Float,
            ParameterAssignment<bool> => ParameterType.Bool,
            _ => throw new ArgumentOutOfRangeException(nameof(assignment), assignment, null)
        },
        Name = assignment.Name,
        IntValue = assignment is ParameterAssignment<int> intAssignment ? intAssignment.Value : default,
        FloatValue = assignment is ParameterAssignment<float> floatAssignment ? floatAssignment.Value : default,
        BoolValue = assignment is ParameterAssignment<bool> boolAssignment ? boolAssignment.Value : default,
    };
}