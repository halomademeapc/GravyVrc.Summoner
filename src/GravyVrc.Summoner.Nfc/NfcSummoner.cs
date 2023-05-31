using GravyVrc.Summoner.Core;
using System.Text;
using System.Text.RegularExpressions;

namespace GravyVrc.Summoner.Nfc;
public partial class NfcSummoner : IDisposable
{
    public event SummonerTagEventHandler? ParameterTagScanned;

    public delegate void SummonerTagEventHandler(ParameterAssignmentBase parameter);

    private readonly INfcReader _reader = new NfcReader();

    [GeneratedRegex(@"^gravyvrc-summoner:((int)\/(-?\d+)|(bool)\/(true|false)|(float)\/(-?\d+\.?\d*))\/(\S+)$")]
    private static partial Regex ParameterRgx();

    public NfcSummoner()
    {
        _reader.CardInserted += OnCardReady;
    }

    private void OnCardReady()
    {
        var cardData = _reader.ReadBlock(2);
        var stringData = Encoding.ASCII.GetString(cardData);
        var match = ParameterRgx().Match(stringData);
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

    private void AnnounceTagMatch<T>(T value, string name) where T : struct, IComparable, IComparable<T>, IEquatable<T>
    {
        if (ParameterTagScanned is not null)
            ParameterTagScanned(new ParameterAssignment<T> { Name = name, Value = value });
    }

    public void StartListening()
    {
        _reader.Watch();
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}
