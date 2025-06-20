using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;

namespace BrokenStatsBackend.src.Repositories;

public class InstanceRepository(AppDbContext context)
{
    private readonly AppDbContext _context = context;

    public async Task AddInstanceAsync(InstanceEntity instance)
    {
        _context.Instances.Add(instance);
        await _context.SaveChangesAsync();
    }
}
