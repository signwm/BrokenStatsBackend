namespace BrokenStatsBackend.Logging
{
    public static class Logger
    {
        private static readonly string baseDir = "data";
        private static readonly string fullLog = Path.Combine(baseDir, "full_log.csv");
        private static readonly string errorLog = Path.Combine(baseDir, "error_log.csv");

        static Logger()
        {
            Directory.CreateDirectory(baseDir);
            if (!File.Exists(fullLog))
                File.WriteAllText(fullLog, "\n");

            if (!File.Exists(errorLog))
                File.WriteAllText(errorLog, "\n");
        }

        public static void Log(string traceId, string payload, string comment = "")
        {
            var log = new Log
            {
                Timestamp = DateTime.Now,
                TraceId = traceId,
                Status = comment,
                Payload = payload
            };

            File.AppendAllText(fullLog, log.ToCsvLine() + Environment.NewLine);
        }

        public static void LogError(string traceId, string payload, string comment = "")
        {
            var log = new Log
            {
                Timestamp = DateTime.Now,
                TraceId = traceId,
                Status = comment,
                Payload = payload
            };

            var line = log.ToCsvLine() + Environment.NewLine;
            File.AppendAllText(fullLog, line);
            File.AppendAllText(errorLog, line);
        }

        public static List<Log> ReadAllLogs()
        {
            return [.. File.ReadAllLines(fullLog)
                       .Where(line => !string.IsNullOrWhiteSpace(line))
                       .Select(Logging.Log.Parse)];
        }
    }
}
