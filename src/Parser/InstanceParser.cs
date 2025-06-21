using BrokenStatsBackend.src.Models;

namespace BrokenStatsBackend.src.Parser;

public static class InstanceParser
{
    public static InstanceEntity? ToInstanceEntity(DateTime timestamp, string raw)
    {
        var parts = raw.Split("[$]");
        if (parts.Length < 2)
            throw new ArgumentException("Invalid instance packet");

        if (parts.Length >= 8 && string.IsNullOrWhiteSpace(parts[7]))
            return null; // Non-boss instance

        if (!long.TryParse(parts[0], out long id))
            throw new ArgumentException("Invalid instance id");

        string name = parts[1].Trim('[', ']');
        if (name.Equals("WIDMOWA HORDA", StringComparison.OrdinalIgnoreCase) && parts.Length >= 10)
        {
            string location = parts[9].Trim();
            if (location.Equals("Miasto Widmowej Hordy", StringComparison.OrdinalIgnoreCase))
            {
                name = "IVRAVUL";
            }
            else if (location.Equals("Miasto Widmowych Uruków", StringComparison.OrdinalIgnoreCase))
            {
                name = "DRAUGUL";
            }
        }
        int difficulty = 1;
        if (parts.Length >= 4 && int.TryParse(parts[3], out int d))
            difficulty = d;

        return new InstanceEntity
        {
            InstanceId = id,
            Name = name,
            Difficulty = difficulty,
            StartTime = timestamp
        };
    }
}
