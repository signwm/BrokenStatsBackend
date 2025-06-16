using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrokenStatsBackend.src.Controllers;

[ApiController]
[Route("api/fights")]
public class FightsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet("flat")]
    public async Task<ActionResult<IEnumerable<FightFlatDto>>> GetFlat(
       int page = 1,
       int pageSize = 50,
       DateTime? startDateTime = null,
       DateTime? endDateTime = null)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest("Invalid page or pageSize");

        var query = _db.Fights
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

        var fights = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = fights.Select(fight => new FightFlatDto
        {
            Time = fight.Time,
            Exp = fight.Exp,
            Gold = fight.Gold,
            Psycho = fight.Psycho,
            Opponents = string.Join(", ",
                fight.Opponents
                    .Select(o => $"{o.OpponentType.Name}({o.OpponentType.Level})")
                    .Distinct()
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
            )
        });

        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
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
                drops = new List<object>(),
                dropValuesPerType = new Dictionary<string, int>()
            });
        }

        var totalExp = fights.Sum(f => f.Exp);
        var totalGold = fights.Sum(f => f.Gold);
        var totalPsycho = fights.Sum(f => f.Psycho);
        var sessionStart = fights.Min(f => f.Time);
        var sessionEnd = fights.Max(f => f.Time);
        var fightsCount = fights.Count;

        var allDrops = fights.SelectMany(f => f.Drops).ToList();

        var dropsSummary = allDrops
            .GroupBy(d => d.DropItem.Name)
            .Select(g => new
            {
                name = g.Key,
                count = g.Sum(d => d.Quantity)
            })
            .ToList();

        var dropValuesPerType = allDrops
            .GroupBy(d => d.DropItem.DropType.Type)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(d => d.DropItem.Value * d.Quantity)
            );

        return Ok(new
        {
            totalExp,
            totalGold,
            totalPsycho,
            fightsCount,
            sessionStart,
            sessionEnd,
            drops = dropsSummary,
            dropValuesPerType
        });
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
