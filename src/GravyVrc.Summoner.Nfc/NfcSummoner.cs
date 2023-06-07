using System.Text;
using GravyVrc.Summoner.Core;
using PCSC;
using PCSC.Monitoring;
using System.Text.RegularExpressions;

namespace GravyVrc.Summoner.Nfc;

public partial class NfcSummoner : IDisposable
{
    public event SummonerTagEventHandler? ParameterTagScanned;

    public delegate void SummonerTagEventHandler(ParameterAssignmentBase parameter);

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

    private void MonitorOnStatusChanged(object sender, StatusChangeEventArgs e)
    {
        if (e.NewState != SCRState.Present)
            return;
        Console.WriteLine($"New state: {e.NewState}");
        try
        {
            using var reader = _context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any);
            // var uid = reader.HandleIso14443_3Tag();
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

public struct Uid
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

public ref struct TransmissionResponse
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

public ref struct NfcData
{
    private readonly ReadOnlySpan<byte> RawData;

    public NfcData(ReadOnlySpan<byte> rawData)
    {
        RawData = rawData;
    }

    private const char UnicodeTerminator = '�';

    public override string ToString()
    {
        var parsed = Encoding.UTF8.GetString(RawData[7..]);
        var terminatorIndex = parsed.IndexOf(UnicodeTerminator);
        return parsed[..terminatorIndex];
    }
}