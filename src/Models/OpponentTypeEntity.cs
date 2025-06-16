namespace BrokenStatsBackend.src.Models
{
    public class OpponentTypeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }

        public List<OpponentEntity> Appearances { get; set; } = new();
    }
}