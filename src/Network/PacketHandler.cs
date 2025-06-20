
using System.Text;

namespace BrokenStatsBackend.src.Network
{
    public class PacketHandler
    {
        private class MessageDefinition(string name, string startMarker, Action<DateTime, string, string> consumer)
        {
            public readonly string Name = name;
            public readonly string StartMarkerString = startMarker;
            public readonly byte[] StartMarker = Encoding.ASCII.GetBytes(startMarker);

            public readonly Action<DateTime, string, string> Consumer = consumer;
        }

        private readonly List<MessageDefinition> definitions = [];
        private readonly List<byte> buffer = [];

        public void RegisterBuffer(string name, string startMarker, Action<DateTime, string, string> consumer)
        {
            definitions.Add(new MessageDefinition(name, startMarker, consumer));

        }

        public void HandlePacket(DateTime timestamp, string traceId, byte[] payload)
        {
            buffer.AddRange(payload);
            ProcessBuffer(timestamp, traceId);
        }

        private void ProcessBuffer(DateTime timestamp, string traceId)
        {
            while (true)
            {
                int endIndex = IndexOfByte(buffer, 0x00, 0);
                if (endIndex < 0)
                {
                    TrimBuffer();
                    break;
                }

                byte[] fullMessageBytes = buffer.GetRange(0, endIndex).ToArray();
                buffer.RemoveRange(0, endIndex + 1);

                string fullMessage = Encoding.UTF8.GetString(fullMessageBytes);
                PayloadLogger.SavePayload(fullMessage);

                foreach (var def in definitions)
                {
                    if (fullMessage.StartsWith(def.StartMarkerString))
                    {
                        string content = fullMessage[def.StartMarkerString.Length..];
                        def.Consumer(timestamp, traceId, content);
                        break;
                    }
                }
            }
        }


        private static int IndexOfByte(List<byte> buffer, byte value, int startIndex)
        {
            for (int i = startIndex; i < buffer.Count; i++)
            {
                if (buffer[i] == value)
                    return i;
            }
            return -1;
        }

        private void TrimBuffer()
        {
            const int maxSize = 1024;
            if (buffer.Count > maxSize)
            {
                buffer.RemoveRange(0, buffer.Count - maxSize);
            }
        }
    }
}
