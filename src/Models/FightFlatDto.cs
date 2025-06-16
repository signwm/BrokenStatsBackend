namespace BrokenStatsBackend.src.Models
{
    public class FightFlatDto
    {
        public DateTime Time { get; set; }
        public int Exp { get; set; }
        public int Gold { get; set; }
        public int Psycho { get; set; }
        public string Opponents { get; set; } = string.Empty;
        public string Drops { get; set; } = string.Empty;
    }
}