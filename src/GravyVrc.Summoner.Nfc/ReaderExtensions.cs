using PCSC;

namespace GravyVrc.Summoner.Nfc;

internal static class ReaderExtensions
{
    internal static Uid? HandleIso14443_3Tag(this ICardReader reader)
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

    internal static NfcData Read(this ICardReader reader, byte block, byte length, int blockSize = 4,
        int packetSize = 16,
        byte readClass = 0xff)
    {
        return new(length > packetSize
            ? ReadChunked(reader, block, length, blockSize, packetSize, readClass)
            : ReadSingle(reader, block, length, readClass));


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
                results.Add(ReadSingle(reader, (byte)block, (byte)size, readClass).ToArray());
            }

            return results.SelectMany(r => r).ToArray();
        }


        static ReadOnlySpan<byte> ReadSingle(ICardReader reader, byte blockNumber, byte length, byte readClass = 0xff)
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

    private static TransmissionResponse Transmit(this ICardReader reader, byte[] payload, int maxResponseSize)
    {
        var receiveBuffer = new byte[maxResponseSize];
        var receivedByteLength = reader.Transmit(payload, receiveBuffer);
        return new(receiveBuffer.AsSpan()[..receivedByteLength]);
    }
}