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
}
