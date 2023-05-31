namespace GravyVrc.Summoner.Nfc;

internal interface INfcReader : IDisposable
{
    event NfcReader.CardEventHandler CardEjected;
    event NfcReader.CardEventHandler CardInserted;
    event NfcReader.CardEventHandler DeviceDisconnected;

    bool Connect();
    void Disconnect();
    string GetCardUID();
    List<string> GetReadersList();
    ReadOnlySpan<byte> ReadBlock(byte block);
    string Transmit(byte[] buff);
    void Watch();
    bool WriteBlock(string data, byte block);
}