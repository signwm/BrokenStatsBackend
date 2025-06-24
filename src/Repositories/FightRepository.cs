using Microsoft.EntityFrameworkCore;
using BrokenStatsBackend.src.Models;
using BrokenStatsBackend.src.Database;

namespace BrokenStatsBackend.src.Repositories
{
    public class FightRepository(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task AddFightAsync(FightEntity fight)
        {
            foreach (var opp in fight.Opponents)
            {
                var existingType = await _context.OpponentTypes
                    .FirstOrDefaultAsync(x => x.Name == opp.OpponentType.Name && x.Level == opp.OpponentType.Level);
                if (existingType == null)
                {
                    existingType = opp.OpponentType;
                    _context.OpponentTypes.Add(existingType);
                }
                opp.OpponentType = existingType;
            }

            foreach (var drop in fight.Drops)
            {
                var existingItem = await _context.DropItems
                    .Include(x => x.DropType)
                    .FirstOrDefaultAsync(x => x.Name == drop.DropItem.Name && x.Quality == drop.DropItem.Quality);

                if (existingItem == null)
                {
                    var dropType = await _context.DropTypes
                        .FirstOrDefaultAsync(dt => dt.Type == drop.DropItem.DropType.Type);

                    if (dropType == null)
                    {
                        dropType = drop.DropItem.DropType;
                        _context.DropTypes.Add(dropType);
                    }

                    drop.DropItem.DropType = dropType;
                    existingItem = drop.DropItem;
                    _context.DropItems.Add(existingItem);
                }

                drop.DropItem = existingItem;
            }

            var instance = await _context.Instances
                .FirstOrDefaultAsync(i => fight.Time >= i.StartTime && fight.Time <= (i.EndTime ?? DateTime.MaxValue));
            if (instance != null)
                fight.Instance = instance;

            _context.Fights.Add(fight);
            await _context.SaveChangesAsync();
        }

        public OpponentTypeEntity GetOrCreateOpponentType(string name, int level)
        {
            var existing = _context.OpponentTypes.FirstOrDefault(x => x.Name == name && x.Level == level);
            if (existing != null)
                return existing;

            var created = new OpponentTypeEntity { Name = name, Level = level };
            _context.OpponentTypes.Add(created);
            _context.SaveChanges();
            return created;
        }

        public DropItemEntity GetOrCreateDropItem(string name, string? quality, string dropTypeStr)
        {
            var existing = _context.DropItems
                .Include(d => d.DropType)
                .FirstOrDefault(x => x.Name == name && x.Quality == quality);

            if (existing != null)
                return existing;

            var dropType = _context.DropTypes.FirstOrDefault(x => x.Type == dropTypeStr);
            if (dropType == null)
            {
                dropType = new DropTypeEntity { Type = dropTypeStr };
                _context.DropTypes.Add(dropType);
                _context.SaveChanges();
            }

            var item = new DropItemEntity
            {
                Name = name,
                Quality = quality,
                DropType = dropType
            };
            _context.DropItems.Add(item);
            _context.SaveChanges();
            return item;
        }

        public async Task AssignInstancesToExistingFightsAsync()
        {
            var fights = await _context.Fights
                .Where(f => f.InstanceId == null)
                .ToListAsync();
            if (fights.Count == 0) return;

            var instances = await _context.Instances.ToListAsync();

            foreach (var fight in fights)
            {
                var inst = instances.FirstOrDefault(i => fight.Time >= i.StartTime && fight.Time <= (i.EndTime ?? DateTime.MaxValue));
                if (inst != null)
                    fight.InstanceId = inst.Id;
            }

            await _context.SaveChangesAsync();
        }
    }
}