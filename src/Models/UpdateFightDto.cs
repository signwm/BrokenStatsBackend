namespace BrokenStatsBackend.src.Models;

public class UpdateFightDto
{
    public DateTime Time { get; set; }
    public int Gold { get; set; }
    public int Psycho { get; set; }
    public int Exp { get; set; }
    public int? InstanceId { get; set; }
}
