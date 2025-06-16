namespace BrokenStatsBackend.src.Models
{
    public class FightEntity
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();
        public DateTime Time { get; set; }
        public int Gold { get; set; }
        public int Psycho { get; set; }
        public int Exp { get; set; }

        public List<OpponentEntity> Opponents { get; set; } = new();
        public List<DropEntity> Drops { get; set; } = new();
    }
}