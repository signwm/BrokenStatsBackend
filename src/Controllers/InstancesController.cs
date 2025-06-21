using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrokenStatsBackend.src.Controllers;

[ApiController]
[Route("api/instances")]
public class InstancesController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> GetInstances()
    {
        var instances = await _db.Instances
            .OrderByDescending(i => i.StartTime)
            .ToListAsync();

        var result = new List<object>();

        foreach (var instance in instances)
        {
            var fights = await _db.Fights
                .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
                .Include(f => f.Drops).ThenInclude(d => d.DropItem).ThenInclude(di => di.DropType)
                .Where(f => f.Time >= instance.StartTime && (instance.EndTime == null || f.Time <= instance.EndTime))
                .OrderBy(f => f.Time)
                .ToListAsync();

            var fightDtos = fights.Select(f => new FightFlatDto
            {
                Id = f.PublicId,
                Time = f.Time,
                Exp = f.Exp,
                Gold = f.Gold,
                Psycho = f.Psycho,
                Opponents = string.Join(", ",
                    f.Opponents
                        .GroupBy(o => new { o.OpponentType.Name, o.OpponentType.Level })
                        .Select(g =>
                        {
                            int qty = g.Sum(o => o.Quantity);
                            var amount = qty > 1 ? $" ({qty})" : "";
                            return $"{g.Key.Name}({g.Key.Level}){amount}";
                        })
                ),
                Drops = string.Join(", ",
                    f.Drops
                        .Where(d => d.DropItem != null && d.DropItem.DropType != null)
                        .OrderBy(d => DropTypeOrder(d.DropItem.DropType.Type))
                        .ThenBy(d => d.DropItem.Name)
                        .Select(d =>
                        {
                            var quality = string.IsNullOrWhiteSpace(d.DropItem.Quality) ? "" : $"[{d.DropItem.Quality}]";
                            var amount = d.Quantity > 1 ? $" ({d.Quantity})" : "";
                            return $"{d.DropItem.Name}{quality}{amount}";
                        })
                )
            }).ToList();

            result.Add(new
            {
                id = instance.Id,
                name = instance.Name,
                difficulty = instance.Difficulty,
                startTime = instance.StartTime,
                endTime = instance.EndTime,
                fights = fightDtos
            });
        }

        var grouped = result
            .GroupBy(i => ((DateTime)i.startTime).Date)
            .Select(g => new
            {
                day = g.Key.ToString("yyyy-MM-dd"),
                instances = g.ToList()
            })
            .OrderByDescending(g => g.day)
            .ToList();

        return Ok(grouped);
    }

    private static int DropTypeOrder(string type) => type.ToLower() switch
    {
        "rare" => 0,
        "synergetic" => 1,
        "drif" => 2,
        "item" => 3,
        "trash" => 4,
        _ => 5
    };
}
