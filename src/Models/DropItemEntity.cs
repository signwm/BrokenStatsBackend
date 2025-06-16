namespace BrokenStatsBackend.src.Models
{
    public class DropItemEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Quality { get; set; }
        public int? Value { get; set; }
        public int DropTypeId { get; set; }
        public DropTypeEntity DropType { get; set; } = null!;
        public List<DropEntity> DroppedIn { get; set; } = new();
    }
}