using GravyVrc.Summoner.Core;
using PCSC;
using PCSC.Monitoring;
using System.Text.RegularExpressions;
using Vogen;

namespace GravyVrc.Summoner.Nfc;

public partial class NfcSummoner : IDisposable
{
    public event SummonerTagEventHandler? ParameterTagScanned;

    public delegate void SummonerTagEventHandler(ParameterAssignmentBase parameter);

    private ISCardMonitor? _monitor;
    private ISCardContext _context;

    [GeneratedRegex(@"^gravyvrc-summoner:((int)\/(-?\d+)|(bool)\/(true|false)|(float)\/(-?\d+\.?\d*))\/(\S+)$")]
    private static partial Regex ParameterRgx();

    public NfcSummoner()
    {
        var contextFactory = ContextFactory.Instance;
        _context = contextFactory.Establish(SCardScope.System);
    }

    private void AnnounceTagMatch<T>(T value, string name) where T : struct, IComparable, IComparable<T>, IEquatable<T>
    {
        if (ParameterTagScanned is not null)
            ParameterTagScanned(new ParameterAssignment<T> { Name = name, Value = value });
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
        using var reader = _context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any);
        HandleIso14443_3Tag(reader);
    }

    private Uid? HandleIso14443_3Tag(ICardReader reader)
    {
        var packet = new byte[]
        {
            0xff, // Class
            0xca, // INS
            0x00, // P1: Get current card UID
            0x00, // P2
            0x00, // Le: Full Length of UID
        };
        var response = Transmit(reader, packet, 12);
        if (response.Length < 2)
        {
            // something wrong
            return null;
        }

        // last 2 bytes are status code
        var statusCode = (ushort)BitConverter.ToInt16(response.AsSpan()[^2..]);

        if (statusCode != 144)
        {
            // something wrong
            //return null;
        }

        return new Uid(response[..^2]);
    }

    private byte[] Transmit(ICardReader reader, byte[] payload, int maxResponseSize)
    {
        try
        {
            var receiveBuffer = new byte[maxResponseSize];
            var receivedByteLength = reader.Transmit(payload, receiveBuffer);
            return receiveBuffer[..receivedByteLength];
        }
        catch
        {
            // oops
            return Array.Empty<byte>();
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