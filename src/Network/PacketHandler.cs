
using System.Text;

namespace BrokenStatsBackend.src.Network
{
    public class PacketHandler
    {
        private class MessageDefinition(string name, string startMarker, Action<DateTime, string, string> consumer)
        {
            public readonly string Name = name;
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
            bool found;
            do
            {
                found = false;
                int earliestIndex = -1;
                MessageDefinition? matched = null;

                foreach (var def in definitions)
                {
                    int idx = IndexOfSequence(buffer, def.StartMarker);
                    if (idx >= 0 && (earliestIndex == -1 || idx < earliestIndex))
                    {
                        earliestIndex = idx;
                        matched = def;
                    }
                }

                if (matched == null)
                {
                    TrimBuffer();
                    break;
                }

                if (earliestIndex > 0)
                {
                    buffer.RemoveRange(0, earliestIndex);
                }

                int startContent = matched.StartMarker.Length;
                int endIndex = IndexOfByte(buffer, 0x00, startContent);
                if (endIndex < 0)
                {
                    break;
                }

                byte[] messageBytes = buffer.GetRange(startContent, endIndex - startContent).ToArray();
                buffer.RemoveRange(0, endIndex + 1);

                string message = Encoding.UTF8.GetString(messageBytes);
                PayloadLogger.SavePayload(message);
                matched.Consumer(timestamp, traceId, message);
                found = true;
            } while (found);
        }

        private static int IndexOfSequence(List<byte> buffer, byte[] sequence)
        {
            for (int i = 0; i <= buffer.Count - sequence.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (buffer[i + j] != sequence[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }
            return -1;
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
