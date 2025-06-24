namespace BrokenStatsBackend.src.Models
{
    public class FightFlatDto
    {
        public Guid Id { get; set; }
        public DateTime Time { get; set; }
        public int Exp { get; set; }
        public int Gold { get; set; }
        public int Psycho { get; set; }
        public int DropValue { get; set; }
        public string Opponents { get; set; } = string.Empty;
        public string Drops { get; set; } = string.Empty;
        public string? InstanceName { get; set; }
        public int? InstanceId { get; set; }
    }
}