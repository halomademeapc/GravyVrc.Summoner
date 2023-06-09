using System.Text;
using GravyVrc.Summoner.Core;
using PCSC;
using PCSC.Monitoring;
using System.Text.RegularExpressions;
using MessagePack;

namespace GravyVrc.Summoner.Nfc;

public partial class NfcSummoner : IDisposable
{
    public event TagEventHandler? ParameterTagScanned;
    public event ReaderReadyEventHandler? ReaderReady;

    public delegate void TagEventHandler(IList<ParameterAssignmentBase> parameters);

    public delegate void ReaderReadyEventHandler(ReaderReadyArgs args);

    private static readonly byte[] Signature = { 69, 4, 2, 0 };
    private static readonly byte[] BlankBlock = { 0, 0, 0, 0 };
    private const byte BlockSize = 4;
    private const byte SignatureBlock = 4;
    private const byte HeaderBlock = 5;
    private const byte ContentBlock = 6;

    private ISCardMonitor? _monitor;
    private readonly ISCardContext _context;

    public NfcSummoner()
    {
        var contextFactory = ContextFactory.Instance;
        _context = contextFactory.Establish(SCardScope.System);
    }

    public void StartListening()
    {
        if (_monitor is not null)
            return;

        var readers = _context.GetReaders();

        var monitorFactory = MonitorFactory.Instance;
        _monitor = monitorFactory.Create(SCardScope.System);
        _monitor.StatusChanged += MonitorOnStatusChanged;
        if (readers.Any())
            _monitor.Start(readers);
    }

    public void WriteTag(IEnumerable<ParameterAssignmentBase> assignment, string readerName)
    {
        using var reader = _context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
        var list = assignment.Select(ParameterStorage.FromAssignment).ToList();
        var data = MessagePackSerializer.Serialize(list);
        var header = new Header(data.Length);
        reader.Write(SignatureBlock, Signature);
        reader.Write(HeaderBlock, header.GetBytes());
        reader.Write(ContentBlock, data.Concat(BlankBlock).ToArray());
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
            var receivedSignature = reader.Read(SignatureBlock, BlockSize);
            if (!receivedSignature.Data.SequenceEqual(Signature))
                return; // not ours

            var receivedHeader = reader.Read(HeaderBlock, BlockSize);
            var parsedHeader = new Header(receivedHeader.Data.ToArray());

            var receivedContent = reader.Read(ContentBlock, parsedHeader.DataLength);
            ParseTagData(receivedContent.Data.ToArray());
        }
        catch (Exception)
        {
            // couldn't read that tag, oh well 🤷‍♀️
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _monitor?.Dispose();
    }

    private void ParseTagData(byte[] data)
    {
        var deserialized = MessagePackSerializer.Deserialize<List<ParameterStorage>>(data);
        var assignments = deserialized.Select(s => s.ToAssignment()).ToList();
        ParameterTagScanned?.Invoke(assignments);
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

internal ref struct Header
{
    public int DataLength { get; }

    public Header(byte[] data)
    {
        if (BitConverter.IsLittleEndian)
            data = data.Reverse().ToArray();

        DataLength = BitConverter.ToInt32(data, 0);
    }

    public Header(int dataLength)
    {
        DataLength = dataLength;
    }

    public byte[] GetBytes()
    {
        var intBytes = BitConverter.GetBytes(DataLength);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intBytes);
        return intBytes;
    }
}