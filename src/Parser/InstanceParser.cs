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

        return new InstanceEntity
        {
            InstanceId = id,
            Name = name,
            StartTime = timestamp
        };
    }
}
