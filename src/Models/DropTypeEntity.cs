namespace BrokenStatsBackend.src.Models
{
    public class DropTypeEntity
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;

        public List<DropItemEntity> Drops { get; set; } = new();
    }
}