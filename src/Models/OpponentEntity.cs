namespace BrokenStatsBackend.src.Models
{
    public class OpponentEntity
    {
        public int Id { get; set; }
        public int FightId { get; set; }
        public FightEntity Fight { get; set; } = null!;

        public int OpponentTypeId { get; set; }
        public OpponentTypeEntity OpponentType { get; set; } = null!;
    }
}