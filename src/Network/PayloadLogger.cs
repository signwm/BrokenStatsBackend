using System.Text;

namespace BrokenStatsBackend.src.Network
{
    public static class PayloadLogger
    {
        private static readonly string PacketDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packets");
        private static readonly string DebugLogPath = Path.Combine(PacketDirectory, "payload_debug.log");

        public static void SavePayload(string utf8Text)
        {
            try
            {
                if (string.IsNullOrEmpty(utf8Text))
                    return;

                utf8Text = utf8Text.Replace("\0", "_");

                // Zapis debugowy — żebyś widział, czy w ogóle się odpala
                LogDebug("Received: " + utf8Text);

                // Znajdź prefix (do drugiego ';')
                int firstSemicolon = utf8Text.IndexOf(';');
                if (firstSemicolon == -1)
                {
                    LogDebug("First semicolon not found.");
                    return;
                }

                int secondSemicolon = utf8Text.IndexOf(';', firstSemicolon + 1);
                if (secondSemicolon == -1)
                {
                    LogDebug("Second semicolon not found.");
                    return;
                }

                string prefix = utf8Text.Substring(0, secondSemicolon);
                string safeFilename = prefix.Replace(';', '_') + ".txt";

                // Stwórz folder jeśli trzeba
                if (!Directory.Exists(PacketDirectory))
                    Directory.CreateDirectory(PacketDirectory);

                string path = Path.Combine(PacketDirectory, safeFilename);

                // Dopisz do pliku
                File.AppendAllText(path, utf8Text + Environment.NewLine, Encoding.UTF8);

                LogDebug("Saved to: " + path);
            }
            catch (Exception ex)
            {
                LogDebug("ERROR: " + ex.Message);
            }
        }

        private static void LogDebug(string line)
        {
            try
            {
                if (!Directory.Exists(PacketDirectory))
                    Directory.CreateDirectory(PacketDirectory);

                File.AppendAllText(DebugLogPath, DateTime.Now + " >> " + line + Environment.NewLine);
                Console.WriteLine("PayloadLogger >> " + line);
            }
            catch
            {
                // Ignorujemy błędy debugowania, żeby nie zabić sniffera
            }
        }
    }
}
