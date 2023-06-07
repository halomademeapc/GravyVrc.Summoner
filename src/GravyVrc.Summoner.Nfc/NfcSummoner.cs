using System.Text;
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
        var uid = HandleIso14443_3Tag(reader);
        var content = Read(reader, 4, 96);
        var stringContent = content.ToString();
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

        return new Uid(response.Content.ToArray());
    }

    private NfcData Read(ICardReader reader, byte block, byte length, int blockSize = 4, int packetSize = 16,
        byte readClass = 0xff)
    {
        return new(length > packetSize
            ? ReadChunked(reader, block, length, blockSize, packetSize, readClass)
            : ReadSingle(reader, block, length, blockSize, packetSize, readClass));


        /*static IEnumerable<byte> ReadChunked(ICardReader reader, byte block, int length, int blockSize = 4, int packetSize = 16,
            byte readClass = 0xff)
        {
            var remainingLength = length;
            while (remainingLength > 0)
            {
                var distanceToFetch = remainingLength > packetSize ? packetSize : remainingLength;
                
            }
        }*/

        ReadOnlySpan<byte> ReadChunked(ICardReader reader, byte blockNumber, byte length, int blockSize = 4,
            int packetSize = 16, byte readClass = 0xff)
        {
            // just copying from nfc-pcsc right now, too lazy to read all of this
            var p = DivideRoundUp(length, packetSize);
            var results = new List<byte[]>();

            for (var i = 0; i < p; i++)
            {
                var block = blockNumber + ((i * packetSize) / blockSize);
                var size = ((i + 1) * packetSize) < length ? packetSize : length - ((i) * packetSize);
                results.Add(ReadSingle(reader, (byte)block, (byte)size, blockSize, packetSize, readClass).ToArray());
            }

            return results.SelectMany(r => r).ToArray();
        }


        static ReadOnlySpan<byte> ReadSingle(ICardReader reader, byte blockNumber, byte length, int blockSize = 4,
            int packetSize = 16,
            byte readClass = 0xff)
        {
            var packet = new byte[]
            {
                readClass,
                0xb0,
                (byte)((blockNumber >> 8) & 0xff),
                (byte)(blockNumber & 0xff),
                length
            };

            var response = Transmit(reader, packet, length + 2);
            return response.Content;
        }

        int DivideRoundUp(int dividend, int divisor) => (dividend + (divisor - 1)) / divisor;
    }

    private static TransmissionResponse Transmit(ICardReader reader, byte[] payload, int maxResponseSize)
    {
        var receiveBuffer = new byte[maxResponseSize];
        var receivedByteLength = reader.Transmit(payload, receiveBuffer);
        return new(receiveBuffer.AsSpan()[..receivedByteLength]);
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