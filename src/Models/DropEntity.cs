namespace BrokenStatsBackend.src.Models
{
    public class DropEntity
    {
        public int Id { get; set; }
        public int FightId { get; set; }
        public FightEntity Fight { get; set; } = null!;

        public int DropItemId { get; set; }
        public DropItemEntity DropItem { get; set; } = null!;

        public int Quantity { get; set; }
    }
}