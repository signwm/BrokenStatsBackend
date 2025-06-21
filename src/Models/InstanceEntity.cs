using System;
namespace BrokenStatsBackend.src.Models
{
    public class InstanceEntity
    {
        public int Id { get; set; }
        public long InstanceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
