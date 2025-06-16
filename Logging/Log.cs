namespace BrokenStatsBackend.Logging
{
    public class Log
    {
        public DateTime Timestamp { get; set; }
        public string TraceId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Payload { get; set; } = "";

        public static Log Parse(string csvLine)
        {
            // Zakładamy format: ISO_TIMESTAMP,traceId,COMMENT,"payload"
            var parts = SplitCsv(csvLine);

            return new Log
            {
                Timestamp = DateTime.Parse(parts[0]),
                TraceId = parts[1],
                Status = parts[2],
                Payload = parts[3]
                    .Replace("\\n", "\n")
                    .Replace("⸴", ",")
            };
        }

        public string ToCsvLine()
        {
            var safePayload = Payload
                .Replace("\u0000", "")
                .Replace(",", "⸴")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Replace("\"", "\"\"");

            return $"{Timestamp:O},{TraceId},{Status.ToUpperInvariant()},\"{safePayload}\"";
        }

        private static string[] SplitCsv(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var value = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value.Append('"');
                        i++; // Skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(value.ToString());
                    value.Clear();
                }
                else
                {
                    value.Append(c);
                }
            }

            result.Add(value.ToString());
            return [.. result];
        }
    }
}
