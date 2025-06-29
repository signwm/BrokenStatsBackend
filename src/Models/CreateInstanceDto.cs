namespace BrokenStatsBackend.src.Models;

public class CreateInstanceDto
{
    public string Name { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<Guid> FightIds { get; set; } = [];
}
