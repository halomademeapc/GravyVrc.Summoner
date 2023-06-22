namespace GravyVrc.Summoner.Core;

public class ParameterAssignment<T> : ParameterAssignmentBase where T : struct, IComparable, IComparable<T>, IEquatable<T>
{
    public override string Name { get; set; } = null!;

    public T Value { get; set; }

    public override object ObjectValue => Value;

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}

public abstract class ParameterAssignmentBase
{
    public abstract string Name { get; set; }
    public abstract object ObjectValue { get; }
}

public enum ParameterType
{
    Int,
    Bool,
    Float
}