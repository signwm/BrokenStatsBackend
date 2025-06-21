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

    [HttpGet("days")]
    public async Task<IActionResult> GetDays()
    {
        DateTime today = DateTime.Today;
        DateTime monthAgo = today.AddDays(-30);
        var counts = await _db.Instances
            .Where(i => i.StartTime.Date >= monthAgo)
            .GroupBy(i => i.StartTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Date, g => g.Count);
        var result = Enumerable.Range(0, 31)
            .Select(offset => today.AddDays(-offset))
            .Select(d => new
            {
                date = d.ToString("yyyy-MM-dd"),
                count = counts.TryGetValue(d, out var c) ? c : 0
            });
        return Ok(result);
    }

    [HttpGet("byDay")]
    public async Task<IActionResult> GetByDay(DateTime date)
    {
        DateTime day = date.Date;
        DateTime next = day.AddDays(1);
        var list = await _db.Instances
            .Where(i => i.StartTime >= day && i.StartTime < next)
            .OrderBy(i => i.StartTime)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                difficulty = i.Difficulty,
                startTime = i.StartTime,
                endTime = i.EndTime
            }).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}/fights")]
    public async Task<IActionResult> GetFights(int id)
    {
        var instance = await _db.Instances.FindAsync(id);
        if (instance == null) return NotFound();
        var fights = await _db.Fights
            .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
            .Include(f => f.Drops).ThenInclude(d => d.DropItem).ThenInclude(di => di.DropType)
            .Where(f => f.InstanceId == id)
            .OrderBy(f => f.Time)
            .ToListAsync();
        var result = fights.Select(f => new InstanceFightDto
        {
            Id = f.PublicId,
            OffsetSeconds = (int)(f.Time - instance.StartTime).TotalSeconds,
            Exp = f.Exp,
            Gold = f.Gold,
            Psycho = f.Psycho,
            DropValue = f.Drops.Sum(FightsController.GetDropValueStatic),
            Opponents = string.Join(", ",
                f.Opponents
                    .GroupBy(o => o.OpponentType.Name)
                    .Select(g =>
                    {
                        int qty = g.Sum(o => o.Quantity);
                        return qty > 1 ? $"{g.Key} ({qty})" : g.Key;
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
        return Ok(result);
    }

    [HttpGet("without/fights")]
    public async Task<IActionResult> GetFightsWithoutInstance()
    {
        var fights = await _db.Fights
            .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
            .Include(f => f.Drops).ThenInclude(d => d.DropItem).ThenInclude(di => di.DropType)
            .Where(f => f.InstanceId == null)
            .OrderByDescending(f => f.Time)
            .ToListAsync();
        var result = fights.Select(f => new FightFlatDto
        {
            Id = f.PublicId,
            Time = f.Time,
            Exp = f.Exp,
            Gold = f.Gold,
            Psycho = f.Psycho,
            DropValue = f.Drops.Sum(FightsController.GetDropValueStatic),
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
        return Ok(result);
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

