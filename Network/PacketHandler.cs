namespace BrokenStatsBackend.Network
{
    public class PacketHandler
    {
        private readonly List<PacketBuffer> buffers = [];
        private PacketBuffer? activeBuffer = null;

        public void RegisterBuffer(PacketBuffer buffer)
        {
            buffers.Add(buffer);
        }

        public void HandlePacket(DateTime timestamp, string traceId, string hex)
        {
            if (activeBuffer == null)
            {
                foreach (var buffer in buffers)
                {
                    var accepted = buffer.StartPacket(timestamp, hex, traceId, out var finished);
                    if (accepted)
                    {
                        if (!finished)
                        {
                            activeBuffer = buffer;
                        }
                        return;
                    }
                }
            }
            else
            {

                var finished = activeBuffer.ContinuePacket(timestamp, hex, traceId);
                if (finished)
                {
                    activeBuffer = null;
                }
            }


        }
    }
}