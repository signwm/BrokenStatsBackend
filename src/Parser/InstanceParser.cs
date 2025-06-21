using BrokenStatsBackend.src.Models;

namespace BrokenStatsBackend.src.Parser;

public static class InstanceParser
{
    public static InstanceEntity ToInstanceEntity(DateTime timestamp, string raw)
    {
        var parts = raw.Split("[$]");
        if (parts.Length < 2)
            throw new ArgumentException("Invalid instance packet");

        if (!long.TryParse(parts[0], out long id))
            throw new ArgumentException("Invalid instance id");

        string name = parts[1].Trim('[', ']');
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
