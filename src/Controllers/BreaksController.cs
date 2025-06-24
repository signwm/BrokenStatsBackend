using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrokenStatsBackend.src.Controllers;

[ApiController]
[Route("api/breaks")]
public class BreaksController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> GetBreaks()
    {
        var breaks = await _db.Breaks.OrderByDescending(b => b.StartTime).ToListAsync();
        var instances = await _db.Instances.ToListAsync();
        var result = breaks.Select(b => new
        {
            id = b.Id,
            startTime = b.StartTime,
            endTime = b.EndTime,
            instance = instances.FirstOrDefault(i => i.StartTime <= b.StartTime && (i.EndTime ?? DateTime.MaxValue) >= b.EndTime)?.Name
        });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBreak([FromBody] CreateBreakDto dto)
    {
        var entity = new BreakEntity
        {
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };
        _db.Breaks.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { id = entity.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBreak(int id, [FromBody] CreateBreakDto dto)
    {
        var entity = await _db.Breaks.FindAsync(id);
        if (entity == null) return NotFound();
        entity.StartTime = dto.StartTime;
        entity.EndTime = dto.EndTime;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBreak(int id)
    {
        var entity = await _db.Breaks.FindAsync(id);
        if (entity == null) return NotFound();
        _db.Breaks.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
