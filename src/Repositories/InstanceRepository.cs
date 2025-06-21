using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BrokenStatsBackend.src.Repositories;

public class InstanceRepository(AppDbContext context)
{
    private readonly AppDbContext _context = context;

    public async Task AddInstanceAsync(InstanceEntity instance)
    {
        _context.Instances.Add(instance);
        await _context.SaveChangesAsync();
    }

    public async Task SetLastInstanceEndTimeAsync(DateTime endTime)
    {
        var last = await _context.Instances
            .OrderByDescending(i => i.StartTime)
            .FirstOrDefaultAsync();

        if (last != null && last.EndTime == null)
        {
            last.EndTime = endTime;
            await _context.SaveChangesAsync();
        }
    }
}
