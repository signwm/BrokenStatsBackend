using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace BrokenStatsBackend.src.Controllers;

[ApiController]
[Route("api/instances")]
public class InstancesController(AppDbContext db, ILogger<InstancesController> logger) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<InstancesController> _logger = logger;

    [HttpGet("days")]
    public async Task<IActionResult> GetDays()
    {
        // Collect all days that contain any fights so that days without
        // instances are also listed. Then gather counts of instances per day
        // and merge both collections in memory.

        var fightDays = await _db.Fights
            .Select(f => f.Time.Date)
            .Distinct()
            .ToListAsync();

        var instanceCounts = await _db.Instances
            .GroupBy(i => i.StartTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var allDays = fightDays
            .Union(instanceCounts.Select(ic => ic.Date))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var result = allDays.Select(d => new
        {
            date = d.ToString("yyyy-MM-dd"),
            count = instanceCounts.FirstOrDefault(ic => ic.Date == d)?.Count ?? 0
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

    [HttpGet("stats")]
    public async Task<IActionResult> GetInstanceStats()
    {
        var finishedInstances = await _db.Instances
            .Where(i => i.EndTime != null)
            .ToListAsync();

        var fightTotals = await _db.Fights
            .Where(f => f.InstanceId != null)
            .GroupBy(f => f.InstanceId)
            .Select(g => new
            {
                InstanceId = g.Key!.Value,
                Gold = g.Sum(f => f.Gold),
                Exp = g.Sum(f => f.Exp),
                Psycho = g.Sum(f => f.Psycho)
            }).ToListAsync();

        var perRun = finishedInstances
            .Join(fightTotals, i => i.Id, f => f.InstanceId, (i, f) => new
            {
                i.Name,
                Duration = (i.EndTime!.Value - i.StartTime).TotalSeconds,
                f.Gold,
                f.Exp,
                f.Psycho
            })
            .ToList();

        var stats = perRun
            .GroupBy(p => p.Name)
            .Select(g => new
            {
                name = g.Key,
                count = g.Count(),
                avgTime = TimeSpan.FromSeconds(g.Average(x => x.Duration)).ToString("hh\:mm\:ss"),
                avgGold = (int)g.Average(x => x.Gold),
                avgExp = (int)g.Average(x => x.Exp),
                avgPsycho = (int)g.Average(x => x.Psycho)
            })
            .OrderBy(s => s.name)
            .ToList();

        return Ok(stats);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInstance([FromBody] CreateInstanceDto dto)
    {
        _logger.LogInformation("CreateInstance entered: {@dto}", dto);
        var fights = await _db.Fights
            .Where(f => dto.FightIds.Contains(f.PublicId) && f.InstanceId == null)
            .ToListAsync();
        if (fights.Count == 0) return BadRequest("No fights found");

        DateTime start = fights.Min(f => f.Time).AddSeconds(-10);
        DateTime end = fights.Max(f => f.Time);

        long nextId = (_db.Instances.Any()
            ? await _db.Instances.MaxAsync(i => i.InstanceId)
            : 0) + 1;



        var instance = new InstanceEntity
        {
            InstanceId = nextId,
            Name = dto.Name,
            Difficulty = dto.Difficulty,
            StartTime = start,
            EndTime = end
        };

        try
        {
            _db.Instances.Add(instance);
            fights.ForEach(f => f.Instance = instance);
            await _db.SaveChangesAsync();
            return Ok(new { id = instance.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }

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

