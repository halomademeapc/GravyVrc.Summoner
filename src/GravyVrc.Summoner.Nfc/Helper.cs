using System.Text;

namespace GravyVrc.Summoner.Nfc;

internal static class Helper
{
    public static IEnumerable<string> SplitByLength(this string str, int maxLength)
    {
        int index = 0;
        while (true)
        {
            if (index + maxLength >= str.Length)
            {
                yield return str[index..];
                yield break;
            }
            yield return str.Substring(index, maxLength);
            index += maxLength;
        }
    }
    public static byte[] Decode(this byte[] bytes)
    {
        if (bytes.Length == 0) return bytes;
        var i = bytes.Length - 1;
        while (bytes[i] == 0 || bytes[i] == 144)
        {
            i--;
        }
        byte[] copy = new byte[i + 1];
        Array.Copy(bytes, copy, i + 1);
        return copy;
    }

    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}
