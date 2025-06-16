namespace BrokenStatsBackend.Logging
{
    public static class LogProcessor
    {
        private static readonly string baseDir = "data";
        private static readonly string inputFile = Path.Combine(baseDir, "toProcess.csv");

        public static void ReprocessLogs(Action<Log> processAction)
        {
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("Brak pliku do przetworzenia: " + inputFile);
                return;
            }

            var lines = File.ReadAllLines(inputFile)
                            .Where(line => !string.IsNullOrWhiteSpace(line));

            foreach (var line in lines)
            {
                try
                {
                    var log = Log.Parse(line);
                    processAction(log); // np. Logger.Log(log.TraceId, log.Payload, log.Comment);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd parsowania linii: {line}\n{ex.Message}");
                    Logger.LogError("SYSTEM", line, "REPROCESS_ERROR");
                }
            }
        }
    }
}
