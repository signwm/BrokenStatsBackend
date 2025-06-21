using BrokenStatsBackend.src.Repositories;
using BrokenStatsBackend.src.Database;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrokenStatsBackend.src.Parser;

public class InstanceCompletionTracker
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly List<HashSet<string>> _groups =
    [
        new HashSet<string> { "Duch Ognia", "Duch Energii", "Duch Zimna" },
        new HashSet<string> { "Babadek", "Gregorius", "Ghadira" },
        new HashSet<string> { "Mahet", "Tarul" },
        new HashSet<string> { "Lugus", "Morana" },
        new HashSet<string> { "Fyodor", "Gmo" }
    ];

    private readonly Dictionary<string, int> _multiKillBosses = new()
    {
        ["Konstrukt"] = 3,
        ["Osłabiony Konstrukt"] = 3,
    };

    private readonly HashSet<string> _singleBosses = new()
    {
        "Herszt", "Krzyżak", "Ropucha", "Ichtion", "Geomorph", "Bibliotekarz",
        "Obserwator", "Władca Marionetek", "Niedźwiedź", "Garthmog", "Utor Komandor",
        "Duch Zamku", "Modliszka", "Tygrys", "Heurokratos", "Ivravul", "Wendigo",
        "Valdarog", "Jaskółka", "Aqua Regis", "Vough", "Vidvar", "Nidhogg", "Hvar",
        "Angwalf-Htaga", "Mortus", "Draugul", "Jastrzębior", "Selena",
        "Admirał Utoru", "Sidraga"
    };

    private readonly List<HashSet<string>> _progress;
    private readonly Dictionary<string, int> _killCounts;

    public InstanceCompletionTracker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _progress = _groups.Select(g => new HashSet<string>()).ToList();
        _killCounts = _multiKillBosses.Keys.ToDictionary(k => k, k => 0);
    }

    private async Task WithRepository(Func<InstanceRepository, Task> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = new InstanceRepository(ctx);
        await action(repo);
    }

    public async Task StartNewInstanceAsync(DateTime startTime)
    {
        await WithRepository(r => r.SetLastInstanceEndTimeAsync(startTime));

        foreach (var set in _progress)
            set.Clear();
        foreach (var key in _multiKillBosses.Keys.ToList())
            _killCounts[key] = 0;
    }

    public async Task ProcessFightAsync(DateTime time, IEnumerable<string> opponentNames)
    {
        bool end = false;
        var names = opponentNames.ToList();

        for (int i = 0; i < _groups.Count; i++)
        {
            var group = _groups[i];
            foreach (var n in names)
            {
                if (group.Contains(n))
                    _progress[i].Add(n);
            }
            if (_progress[i].SetEquals(group))
                end = true;
        }

        foreach (var n in names)
        {
            if (_multiKillBosses.TryGetValue(n, out int required))
            {
                _killCounts[n]++;
                if (_killCounts[n] >= required)
                    end = true;
            }
        }

        if (!end && names.Any(n => _singleBosses.Contains(n)))
            end = true;

        if (end)
        {
            await WithRepository(r => r.SetLastInstanceEndTimeAsync(time));
        }
    }
}
