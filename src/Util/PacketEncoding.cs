using System.Text;

namespace BrokenStatsBackend.Util
{
    public static class PacketEncoding
    {
        public static string HexToUtf8(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string has invalid length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return Encoding.UTF8.GetString(bytes);
        }

        public static string Utf8ToHex(string utf8)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(utf8);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string has invalid length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return bytes;
        }
    }
}