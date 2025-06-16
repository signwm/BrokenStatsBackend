using System;
using Xunit;
using BrokenStatsBackend.src.Parser;
using BrokenStatsBackend.src.Models;
using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BrokenStatsBackend.Tests;

public class FightParserTests
{
    [Fact]
    public void ToFightEntity_DoesNotTouchDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var repository = new FightRepository(context);

        var raw = new FightParser.RawFightData
        {
            Time = DateTime.UtcNow,
            Exp = 1,
            Gold = 2,
            Psycho = 3,
            Opponents = new() { ("Goblin", 5) },
            RawItems = "",
            RawDrifs = "",
            RawRaresAndSyngs = "",
            RawTrash = ""
        };

        _ = FightParser.ToFightEntity(raw);

        Assert.Empty(context.OpponentTypes);
        Assert.Empty(context.DropItems);
        Assert.Empty(context.Fights);
    }
}
