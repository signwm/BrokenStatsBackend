// namespace BrokenStatsBackend.src.Models
// {
//     public class FightEntity
//     {
//         public int Id { get; set; }

//         public Guid PublicId { get; set; } = Guid.NewGuid();
//         public DateTime Time { get; set; }
//         public int Gold { get; set; }
//         public int Psycho { get; set; }
//         public int Exp { get; set; }

//         public List<OpponentEntity> Opponents { get; set; } = new();
//         public List<DropEntity> Drops { get; set; } = new();
//     }

//     public class OpponentEntity
//     {
//         public int Id { get; set; }
//         public int FightId { get; set; }
//         public FightEntity Fight { get; set; } = null!;

//         public int OpponentTypeId { get; set; }
//         public OpponentTypeEntity OpponentType { get; set; } = null!;
//     }

//     public class OpponentTypeEntity
//     {
//         public int Id { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public int Level { get; set; }

//         public List<OpponentEntity> Appearances { get; set; } = new();
//     }

//     public class DropEntity
//     {
//         public int Id { get; set; }
//         public int FightId { get; set; }
//         public FightEntity Fight { get; set; } = null!;

//         public int DropItemId { get; set; }
//         public DropItemEntity DropItem { get; set; } = null!;

//         public int Quantity { get; set; }
//     }

//     public class DropItemEntity
//     {
//         public int Id { get; set; }
//         public string Name { get; set; } = string.Empty;
//         public string? Quality { get; set; }

//         public int DropTypeId { get; set; }
//         public DropTypeEntity DropType { get; set; } = null!;

//         public List<DropEntity> DroppedIn { get; set; } = new();
//     }
    
//         public class DropTypeEntity
//     {
//         public int Id { get; set; }
//         public string Type { get; set; } = string.Empty;

//         public List<DropItemEntity> Drops { get; set; } = new();
//     }
// }