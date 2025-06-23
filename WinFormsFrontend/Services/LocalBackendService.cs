using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Controllers;

namespace BrokenStatsFrontendWinForms.Services;

public class LocalBackendService(AppDbContext db) : IBackendService
{
    private readonly AppDbContext _db = db;

    public async Task<List<InstanceDto>> GetInstancesAsync(DateTime from, DateTime to)
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

        var result = instances.Select(i =>
        {
            var f = fights.Where(x => x.InstanceId == i.Id).ToList();
            int gold = f.Sum(x => x.Gold);
            int exp = f.Sum(x => x.Exp);
            int psycho = f.Sum(x => x.Psycho);
            int drop = f.SelectMany(x => x.Drops).Sum(FightsController.GetDropValueStatic);
            return new InstanceDto
            {
                id = i.Id,
                startTime = i.StartTime,
                name = i.Name,
                difficulty = i.Difficulty,
                gold = gold,
                exp = exp,
                psycho = psycho,
                profit = gold + drop,
                fights = f.Count,
                durationSeconds = i.EndTime != null ? (int?)(i.EndTime.Value - i.StartTime).TotalSeconds : null
            };
        }).ToList();

        return result;
    }

    public async Task<List<InstanceFightDto>> GetFightsAsync(int instanceId)
    {
        var instance = await _db.Instances.FindAsync(instanceId);
        if (instance == null) return [];

        var fights = await _db.Fights
            .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
            .Include(f => f.Drops).ThenInclude(d => d.DropItem).ThenInclude(di => di.DropType)
            .Where(f => f.InstanceId == instanceId)
            .OrderByDescending(f => f.Time)
            .ToListAsync();

        var result = fights.Select(f => new InstanceFightDto
        {
            id = f.PublicId,
            offsetSeconds = (int)(f.Time - instance.StartTime).TotalSeconds,
            exp = f.Exp,
            gold = f.Gold,
            psycho = f.Psycho,
            dropValue = f.Drops.Sum(FightsController.GetDropValueStatic),
            opponents = string.Join(", ",
                f.Opponents
                    .GroupBy(o => o.OpponentType.Name)
                    .Select(g =>
                    {
                        int qty = g.Sum(o => o.Quantity);
                        return qty > 1 ? $"{g.Key} ({qty})" : g.Key;
                    })
            ),
            drops = string.Join(", ",
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

        return result;
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
