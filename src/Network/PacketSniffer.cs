using BrokenStatsBackend.Logging;
using PacketDotNet;
using SharpPcap;

namespace BrokenStatsBackend.src.Network
{
    public class PacketSniffer
    {
        private ICaptureDevice? device;
        private PacketHandler? packetHandler;
        private bool isCapturing = false;

        public void StartCapturing()
        {
            try
            {
                if (isCapturing)
                {
                    Logger.Log("00000000", "", "ALREADY_CAPTURING");
                    return;
                }

                var devices = CaptureDeviceList.Instance;
                device = devices
                    .FirstOrDefault(d => d.Description.Contains("wi-fi", StringComparison.CurrentCultureIgnoreCase) && !d.Description.Contains("virtual", StringComparison.CurrentCultureIgnoreCase));

                if (device == null)
                {
                    Logger.LogError("00000000", "", "NO_WIFI_INTERFACE");
                    return;
                }

                device.OnPacketArrival += OnPacketArrival;
                device.Open(DeviceModes.Promiscuous, 1000);
                device.Filter = "tcp src port 9365";
                device.StartCapture();
                isCapturing = true;

                Logger.Log("00000000", "", "CAPTURING_STARTED");
            }
            catch (Exception ex)
            {
                Logger.LogError("00000000", ex.Message, "CAPTURING_ERROR");
            }
        }

        public void AddPacketHandler(PacketHandler handler)
        {
            packetHandler = handler;
        }

        private void OnPacketArrival(object sender, PacketCapture packetCapture)
        {

            var raw = packetCapture.GetPacket();
            var parsed = Packet.ParsePacket(raw.LinkLayerType, raw.Data);

            var tcpPacket = parsed.Extract<TcpPacket>();
            var ipPacket = parsed.Extract<IPPacket>();

            var traceId = Guid.NewGuid().ToString()[..8];

            if (tcpPacket != null && ipPacket != null && tcpPacket.PayloadData.Length > 0)
            {
                var data = tcpPacket.PayloadData;
                if (!(data.Length == 3 && data[0] == 0x39 && data[1] == 0x39 && data[2] == 0x00))
                {
                    DateTime timestamp = DateTime.Now;
                    packetHandler?.HandlePacket(timestamp, traceId, data);
                }
            }

        }
    }
}
