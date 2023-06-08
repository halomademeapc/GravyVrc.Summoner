using System.Text;
using GravyVrc.Summoner.Core;
using PCSC;
using PCSC.Monitoring;
using System.Text.RegularExpressions;

namespace GravyVrc.Summoner.Nfc;

public partial class NfcSummoner : IDisposable
{
    public event TagEventHandler? ParameterTagScanned;
    public event ReaderReadyEventHandler? ReaderReady;

    public delegate void TagEventHandler(ParameterAssignmentBase parameter);

    public delegate void ReaderReadyEventHandler(ReaderReadyArgs args);

    private ISCardMonitor? _monitor;
    private readonly ISCardContext _context;

    [GeneratedRegex(@"^gravyvrc-summoner:((int)\/(-?\d+)|(bool)\/(true|false)|(float)\/(-?\d+\.?\d*))\/(\S+)$")]
    private static partial Regex ParameterRgx();

    public NfcSummoner()
    {
        var contextFactory = ContextFactory.Instance;
        _context = contextFactory.Establish(SCardScope.System);
    }

    private void AnnounceTagMatch<T>(T value, string name) where T : struct, IComparable, IComparable<T>, IEquatable<T>
    {
        ParameterTagScanned?.Invoke(new ParameterAssignment<T> { Name = name, Value = value });
    }

    public void StartListening()
    {
        if (_monitor is not null)
            return;

        var readers = _context.GetReaders();

        var monitorFactory = MonitorFactory.Instance;
        _monitor = monitorFactory.Create(SCardScope.System);
        _monitor.StatusChanged += MonitorOnStatusChanged;
        _monitor.Start(readers);
    }

    public void WriteTag(ParameterAssignmentBase assignment, string readerName)
    {
        using var reader = _context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
        var type = assignment switch
        {
            ParameterAssignment<int> => "int",
            ParameterAssignment<bool> => "bool",
            ParameterAssignment<float> => "float",
            _ => throw new NotSupportedException("Invalid parameter type")
        };
        var uri = $"gravyvrc-summoner:{type}/{assignment.ObjectValue}/{assignment.Name}";
        var data = new NfcUriData(uri);
        reader.Write(4, data.Data.ToArray());
    }

    private void MonitorOnStatusChanged(object sender, StatusChangeEventArgs e)
    {
        ReaderReady?.Invoke(new ReaderReadyArgs(e.ReaderName, e.NewState.HasFlag(SCRState.Present)));
        if (e.NewState != SCRState.Present)
            return;

        Console.WriteLine($"New state: {e.NewState}");
        try
        {
            using var reader = _context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any);
            var content = reader.Read(4, 96);
            var stringContent = content.ToString();
            ParseTagData(stringContent);
        }
        catch (Exception exception)
        {
            // couldn't read that tag, oh well 🤷‍♀️
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _monitor?.Dispose();
    }

    private void ParseTagData(string data)
    {
        var match = ParameterRgx().Match(data);
        if (!match.Success)
            return;

        var parameterName = match.Groups[8].Value;

        if (match.Groups[2].Success && int.TryParse(match.Groups[3].Value, out var parsedInt))
            AnnounceTagMatch(parsedInt, parameterName);
        else if (match.Groups[4].Success && bool.TryParse(match.Groups[5].Value, out var parsedBool))
            AnnounceTagMatch(parsedBool, parameterName);
        else if (match.Groups[6].Success && float.TryParse(match.Groups[7].Value, out var parsedFloat))
            AnnounceTagMatch(parsedFloat, parameterName);
    }
}

public record struct ReaderReadyArgs(string ReaderName, bool IsReady);

internal struct Uid
{
    public Uid(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public override string ToString() => BitConverter.ToString(Value);

    public override bool Equals(object? obj) => obj is Uid uid && uid.Value.SequenceEqual(Value);

    public bool Equals(Uid other) => other.Value.SequenceEqual(Value);

    public static bool operator ==(Uid a, Uid b) => a.Equals(b);

    public static bool operator !=(Uid a, Uid b) => !a.Equals(b);

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

internal ref struct TransmissionResponse
{
    private readonly ReadOnlySpan<byte> _value;

    public TransmissionResponse(ReadOnlySpan<byte> value)
    {
        if (value.Length < 2)
            throw new ArgumentException("Response must be at least two bytes", nameof(value));
        _value = value;
    }

    public bool IsSuccess() => (ushort)BitConverter.ToInt16(StatusCode) == 144;

    public ReadOnlySpan<byte> StatusCode => _value[^2..];

    public ReadOnlySpan<byte> Content => _value[..^2];
}

internal ref struct NfcUriData
{
    public readonly ReadOnlySpan<byte> Data;

    public NfcUriData(ReadOnlySpan<byte> data)
    {
        Data = data;
    }

    public NfcUriData(string uri)
    {
        var fullString = $"{uri}{UnicodeTerminator}";
        Data = Prefix.Concat(Encoding.UTF8.GetBytes(fullString)).ToArray();
    }

    public const char UnicodeTerminator = '�';

    private static readonly byte[] Prefix =
    {
        3, 41, 209, 1, 37, 85, 0
    };

    public override string ToString()
    {
        var parsed = Encoding.UTF8.GetString(Data[7..]);
        var terminatorIndex = parsed.IndexOf(UnicodeTerminator);
        return parsed[..terminatorIndex];
    }
}