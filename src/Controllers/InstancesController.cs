using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using BrokenStatsBackend.src.Repositories;
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

    private async Task<int> GetBreakSecondsAsync(DateTime start, DateTime end)
    {
        var list = await _db.Breaks
            .Where(b => b.StartTime < end && b.EndTime > start)
            .ToListAsync();
        return list.Sum(b =>
        {
            var from = b.StartTime > start ? b.StartTime : start;
            var to = b.EndTime < end ? b.EndTime : end;
            return (int)(to - from).TotalSeconds;
        });
    }

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
            .OrderByDescending(i => i.StartTime)
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
            .OrderByDescending(f => f.Time)
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
    public async Task<IActionResult> GetFightsWithoutInstance(DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Fights
            .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
            .Include(f => f.Drops).ThenInclude(d => d.DropItem).ThenInclude(di => di.DropType)
            .Where(f => f.InstanceId == null)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(f => f.Time >= from.Value);

        if (to.HasValue)
            query = query.Where(f => f.Time <= to.Value);

        var fights = await query
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

        var fightsWithDrops = await _db.Fights
            .Where(f => f.InstanceId != null)
            .Include(f => f.Drops)
                .ThenInclude(d => d.DropItem)
                    .ThenInclude(di => di.DropType)
            .ToListAsync();

        var fightTotals = fightsWithDrops
            .GroupBy(f => f.InstanceId!.Value)
            .Select(g => new
            {
                InstanceId = g.Key,
                Gold = g.Sum(f => f.Gold),
                Exp = g.Sum(f => f.Exp),
                Psycho = g.Sum(f => f.Psycho),
                DropValue = g.SelectMany(f => f.Drops)
                    .Sum(d => FightsController.GetDropValueStatic(d))
            })
            .ToList();

        var perRun = new List<dynamic>();
        foreach (var i in finishedInstances)
        {
            var f = fightTotals.First(ft => ft.InstanceId == i.Id);
            int breakSec = await GetBreakSecondsAsync(i.StartTime, i.EndTime!.Value);
            var dur = (int)(i.EndTime!.Value - i.StartTime).TotalSeconds - breakSec;
            perRun.Add(new
            {
                i.Name,
                i.Difficulty,
                Duration = dur < 0 ? 0 : dur,
                f.Gold,
                f.Exp,
                f.Psycho,
                f.DropValue
            });
        }

        var stats = perRun
            .GroupBy(p => new { p.Name, p.Difficulty })
            .Select(g => new
            {
                name = g.Key.Name,
                difficulty = g.Key.Difficulty,
                count = g.Count(),
                avgTime = TimeSpan.FromSeconds(g.Average(x => x.Duration)).ToString(@"hh\:mm\:ss"),
                avgGold = (int)g.Average(x => x.Gold),
                avgExp = (int)g.Average(x => x.Exp),
                avgPsycho = (int)g.Average(x => x.Psycho),
                avgProfit = (int)g.Average(x => x.Gold + x.DropValue)
            })
            .OrderBy(s => s.name)
            .ThenBy(s => s.difficulty)
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

        DateTime start = dto.StartTime != default
            ? dto.StartTime
            : fights.Min(f => f.Time).AddSeconds(-10);
        DateTime end = dto.EndTime != default
            ? dto.EndTime
            : fights.Max(f => f.Time);

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

    [HttpPost("closeLast")]
    public async Task<IActionResult> CloseLastInstance()
    {
        var repo = new InstanceRepository(_db);
        await repo.SetLastInstanceEndTimeAsync(DateTime.Now);
        return Ok();
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> CloseInstance(int id)
    {
        var instance = await _db.Instances.FindAsync(id);
        if (instance == null) return NotFound();
        if (instance.EndTime == null)
        {
            instance.EndTime = DateTime.Now;
            await _db.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInstance(int id)
    {
        var instance = await _db.Instances.FindAsync(id);
        if (instance == null) return NotFound();

        var fights = await _db.Fights.Where(f => f.InstanceId == id).ToListAsync();
        fights.ForEach(f => f.InstanceId = null);
        _db.Instances.Remove(instance);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("range")]
    public async Task<IActionResult> GetInstancesInRange(DateTime from, DateTime to)
    {
        var instances = await _db.Instances
            .Where(i => i.StartTime >= from && i.StartTime <= to)
            .OrderByDescending(i => i.StartTime)
            .ToListAsync();

        var ids = instances.Select(i => i.Id).ToList();

        var fights = await _db.Fights
            .Where(f => f.InstanceId != null && ids.Contains(f.InstanceId.Value))
            .Include(f => f.Drops)
                .ThenInclude(d => d.DropItem)
                    .ThenInclude(di => di.DropType)
            .ToListAsync();

        var result = new List<object>();
        foreach (var i in instances)
        {
            var f = fights.Where(x => x.InstanceId == i.Id).ToList();
            int gold = f.Sum(x => x.Gold);
            int exp = f.Sum(x => x.Exp);
            int psycho = f.Sum(x => x.Psycho);
            int drop = f.SelectMany(x => x.Drops).Sum(d => FightsController.GetDropValueStatic(d));
            int? duration = null;
            if (i.EndTime != null)
            {
                int breakSec = await GetBreakSecondsAsync(i.StartTime, i.EndTime.Value);
                int dur = (int)(i.EndTime.Value - i.StartTime).TotalSeconds - breakSec;
                if (dur < 0) dur = 0;
                duration = dur;
            }
            result.Add(new
            {
                id = i.Id,
                startTime = i.StartTime,
                name = i.Name,
                difficulty = i.Difficulty,
                gold,
                exp,
                psycho,
                profit = gold + drop,
                fights = f.Count,
                durationSeconds = duration
            });
        }

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

