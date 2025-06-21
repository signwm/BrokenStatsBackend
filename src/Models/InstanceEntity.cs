using System;
namespace BrokenStatsBackend.src.Models
{
    public class InstanceEntity
    {
        public int Id { get; set; }
        public long InstanceId { get; set; }
        public string Name { get; set; } = string.Empty;
        // Difficulty level of the instance:
        // 1 - Normal, 2 - Easy, 3 - Hard
        public int Difficulty { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
