using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace BrokenStatsBackend.src.Controllers;

[ApiController]
[Route("api/fights")]
public class FightsController(AppDbContext db, ILogger<FightsController> logger) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<FightsController> _logger = logger;

    [HttpGet("flat")]
    public async Task<ActionResult<IEnumerable<FightFlatDto>>> GetFlat(
       DateTime? startDateTime = null,
       DateTime? endDateTime = null,
       string? search = null)
    {
        try
        {
        var query = _db.Fights
            .Include(f => f.Instance)
            .Include(f => f.Opponents).ThenInclude(o => o.OpponentType)
            .Include(f => f.Drops).ThenInclude(d => d.DropItem).ThenInclude(di => di.DropType)
            .AsQueryable();

        // 🔍 Filtrowanie po czasie
        if (startDateTime.HasValue)
            query = query.Where(f => f.Time >= startDateTime.Value);

        if (endDateTime.HasValue)
            query = query.Where(f => f.Time <= endDateTime.Value);

        // 📅 Sortowanie najnowsze pierwsze
        query = query.OrderByDescending(f => f.Time);

        var fights = await query.ToListAsync();

        var result = fights.Select(fight => new FightFlatDto
        {
            Id = fight.PublicId,
            Time = fight.Time,
            Exp = fight.Exp,
            Gold = fight.Gold,
            Psycho = fight.Psycho,
            DropValue = fight.Drops.Sum(GetDropValue),
            Opponents = string.Join(", ",
                fight.Opponents
                    .GroupBy(o => new { o.OpponentType.Name, o.OpponentType.Level })
                    .Select(g =>
                    {
                        int qty = g.Sum(o => o.Quantity);
                        var amount = qty > 1 ? $" ({qty})" : "";
                        return $"{g.Key.Name}({g.Key.Level}){amount}";
                    })
            ),
            Drops = string.Join(", ",
                fight.Drops
                    .Where(d => d.DropItem != null && d.DropItem.DropType != null)
                    .OrderBy(d => DropTypeOrder(d.DropItem.DropType.Type))
                    .ThenBy(d => d.DropItem.Name)
                    .Select(d =>
                    {
                        var quality = string.IsNullOrWhiteSpace(d.DropItem.Quality) ? "" : $"[{d.DropItem.Quality}]";
                        var amount = d.Quantity > 1 ? $" ({d.Quantity})" : "";
                        return $"{d.DropItem.Name}{quality}{amount}";
                    })
            ),
            InstanceName = fight.Instance != null ? fight.Instance.Name : null,
            InstanceId = fight.InstanceId
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            result = result.Where(r =>
                r.Time.ToString().ToLower().Contains(s) ||
                r.Exp.ToString().Contains(s) ||
                r.Gold.ToString().Contains(s) ||
                r.Psycho.ToString().Contains(s) ||
                r.Opponents.ToLower().Contains(s) ||
                r.Drops.ToLower().Contains(s)
            ).ToList();
        }

        return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fights");
            throw;
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try
        {
        var fights = await _db.Fights
            .Include(f => f.Drops)
                .ThenInclude(d => d.DropItem)
                    .ThenInclude(di => di.DropType)
            .Where(f => f.Time >= from && f.Time <= to)
            .ToListAsync();

        if (fights.Count == 0)
        {
            return Ok(new
            {
                totalExp = 0,
                totalGold = 0,
                totalPsycho = 0,
                fightsCount = 0,
                sessionStart = (DateTime?)null,
                sessionEnd = (DateTime?)null,
                items = new List<object>(),
                drifs = new List<object>(),
                synergetics = new List<object>(),
                trash = new List<object>(),
                rare = new List<object>(),
                dropValuesPerType = new Dictionary<string, int>(),
                totalGoldWithDrops = 0
            });
        }

        var totalExp = fights.Sum(f => f.Exp);
        var totalGold = fights.Sum(f => f.Gold);
        var totalPsycho = fights.Sum(f => f.Psycho);
        var sessionStart = fights.Min(f => f.Time);
        var sessionEnd = fights.Max(f => f.Time);
        var fightsCount = fights.Count;

        var allDrops = fights.SelectMany(f => f.Drops).ToList();

        var items = GroupByName(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "item"));
        var drifs = GroupByName(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "drif"));
        var synergetics = GroupBySynergeticSuffix(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "synergetic"));
        var trash = GroupByQuality(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "trash"));
        var rare = GroupByName(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "rare"));

        var dropValuesPerType = allDrops
            .GroupBy(d => d.DropItem.DropType.Type)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(GetDropValue)
            );

        var totalGoldWithDrops = totalGold + dropValuesPerType.Values.Sum();

        return Ok(new
        {
            totalExp,
            totalGold,
            totalGoldWithDrops,
            totalPsycho,
            fightsCount,
            sessionStart,
            sessionEnd,
            items,
            drifs,
            synergetics,
            trash,
            rare,
            dropValuesPerType
        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get summary");
            throw;
        }
    }

    [HttpPost("summary")]
    public async Task<IActionResult> GetSummaryByIds([FromBody] Guid[] ids)
    {
        try
        {
        var fights = await _db.Fights
            .Include(f => f.Drops)
                .ThenInclude(d => d.DropItem)
                    .ThenInclude(di => di.DropType)
            .Where(f => ids.Contains(f.PublicId))
            .ToListAsync();

        if (fights.Count == 0)
        {
            return Ok(new
            {
                totalExp = 0,
                totalGold = 0,
                totalPsycho = 0,
                fightsCount = 0,
                sessionStart = (DateTime?)null,
                sessionEnd = (DateTime?)null,
                items = new List<object>(),
                drifs = new List<object>(),
                synergetics = new List<object>(),
                trash = new List<object>(),
                rare = new List<object>(),
                dropValuesPerType = new Dictionary<string, int>(),
                totalGoldWithDrops = 0
            });
        }

        var totalExp = fights.Sum(f => f.Exp);
        var totalGold = fights.Sum(f => f.Gold);
        var totalPsycho = fights.Sum(f => f.Psycho);
        var sessionStart = fights.Min(f => f.Time);
        var sessionEnd = fights.Max(f => f.Time);
        var fightsCount = fights.Count;

        var allDrops = fights.SelectMany(f => f.Drops).ToList();

        var items = GroupByName(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "item"));
        var drifs = GroupByName(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "drif"));
        var synergetics = GroupBySynergeticSuffix(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "synergetic"));
        var trash = GroupByQuality(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "trash"));
        var rare = GroupByName(allDrops.Where(d => d.DropItem.DropType.Type.ToLower() == "rare"));

        var dropValuesPerType = allDrops
            .GroupBy(d => d.DropItem.DropType.Type)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(GetDropValue)
            );

        var totalGoldWithDrops = totalGold + dropValuesPerType.Values.Sum();

        return Ok(new
        {
            totalExp,
            totalGold,
            totalGoldWithDrops,
            totalPsycho,
            fightsCount,
            sessionStart,
            sessionEnd,
            items,
            drifs,
            synergetics,
            trash,
            rare,
            dropValuesPerType
        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get summary by ids");
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFight(Guid id, [FromBody] UpdateFightDto dto)
    {
        var fight = await _db.Fights.FirstOrDefaultAsync(f => f.PublicId == id);
        if (fight == null) return NotFound();
        fight.Time = dto.Time;
        fight.Gold = dto.Gold;
        fight.Psycho = dto.Psycho;
        fight.Exp = dto.Exp;
        fight.InstanceId = dto.InstanceId;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFight(Guid id)
    {
        var fight = await _db.Fights.FirstOrDefaultAsync(f => f.PublicId == id);
        if (fight == null) return NotFound();
        _db.Fights.Remove(fight);
        await _db.SaveChangesAsync();
        return Ok();
    }


    public static int GetDropValueStatic(DropEntity drop) => GetDropValue(drop);

    private static int GetDropValue(DropEntity drop)
    {
        string type = drop.DropItem.DropType.Type.ToLower();
        return type switch
        {
            "synergetic" => GetSynergeticPrice(drop.DropItem.Name) * drop.Quantity,
            "trash" => GetTrashPrice(drop.DropItem.Quality) * drop.Quantity,
            _ => drop.DropItem.Value.GetValueOrDefault() * drop.Quantity
        };
    }

    private static int GetSynergeticPrice(string name)
    {
        var lastWord = name.Split(' ').Last();
        if (Config.SynergeticSuffixPrices.TryGetValue(lastWord, out var price))
            return price;
        return Config.SynergeticDefaultPrice;
    }

    private static int GetTrashPrice(string? quality)
    {
        if (quality != null && Config.TrashQualityPrices.TryGetValue(quality, out var price))
            return price;
        return Config.TrashDefaultPrice;
    }

    private static List<object> GroupByName(IEnumerable<DropEntity> drops) =>
        drops
            .GroupBy(d => d.DropItem.Name)
            .OrderByDescending(g => g.Sum(d => d.Quantity))
            .Select(g => (object)new { name = g.Key, count = g.Sum(d => d.Quantity) })
            .ToList();

    private static List<object> GroupByQuality(IEnumerable<DropEntity> drops) =>
        drops
            .GroupBy(d => d.DropItem.Quality ?? "?")
            .OrderByDescending(g => g.Sum(d => d.Quantity))
            .Select(g => (object)new { name = g.Key, count = g.Sum(d => d.Quantity) })
            .ToList();

    private static List<object> GroupBySynergeticSuffix(IEnumerable<DropEntity> drops) =>
        drops
            .GroupBy(d => d.DropItem.Name.Split(' ').Last())
            .OrderByDescending(g => g.Sum(d => d.Quantity))
            .Select(g => (object)new { name = g.Key, count = g.Sum(d => d.Quantity) })
            .ToList();





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
