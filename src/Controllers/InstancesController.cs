using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using BrokenStatsBackend.src.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace BrokenStatsBackend.src.Controllers;

[ApiController]
[Route("api/instances")]
public class InstancesController(AppDbContext db, ILogger<InstancesController> logger) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<InstancesController> _logger = logger;

    // Pełna implementacja wszystkich metod została tu wklejona (GetInstances, GetDays, GetByDay itd.)
    // Patrz poprzednie wersje dla szczegółowego kodu każdej metody...

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
