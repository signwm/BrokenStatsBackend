using BrokenStatsBackend.Logging;
using BrokenStatsBackend.Util;

namespace BrokenStatsBackend.Network
{
    public class PacketBuffer(string name, string startMarker, Action<DateTime, string, string> consumer)
    {
        private readonly string Name = name;
        public readonly string startMarker = startMarker.ToLowerInvariant();
        private readonly List<string> parts = [];
        public Action<DateTime, string, string> Consumer { get; } = consumer;

        public bool ContinuePacket(DateTime timestamp, string hex, string traceId)
        {
            Logger.Log(traceId, hex, $"{Name}_PACKET_CONTINUATION");

            parts.Add(hex);

            if (hex.Contains("-00"))
            {
                Finish(timestamp, traceId);
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool StartPacket(DateTime timestamp, string hex, string traceId, out bool completed)
        {
            completed = false;

            int index = hex.IndexOf(startMarker);

            if (index < 0) return false;

            Logger.Log(traceId, hex, $"{Name}_PACKET_STARTED");

            string part = hex[(index + startMarker.Length)..];

            parts.Clear();
            parts.Add(part);

            if (part.Contains("-00"))
            {
                Finish(timestamp, traceId);
                completed = true;
            }

            return true;
        }

        private void Finish(DateTime timestamp, string traceId)
        {
            try
            {
                string completedData = string.Join("", parts);
                int endIndex = completedData.IndexOf("-00", StringComparison.Ordinal);

                var finalHex = completedData[..endIndex];

                Logger.Log(traceId, finalHex, $"{Name}_PACKET_COMPLETED");

                string utf8 = PacketEncoding.HexToUtf8(finalHex.Replace("-", ""));
                Consumer(timestamp, traceId, utf8);
            }
            catch (Exception ex)
            {
                Logger.LogError(traceId, ex.Message, "ERROR");
            }
        }

    }
}
