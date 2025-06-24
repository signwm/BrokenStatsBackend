using BrokenStatsBackend;
using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Repositories;
using BrokenStatsBackend.src.Parser;
using Microsoft.EntityFrameworkCore;
using BrokenStatsBackend.src.Network;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Ensure the SQLite data directory exists
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "data"));

// 🔧 DB i zależności
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=data/data.db"));
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Log all unhandled exceptions so that they appear in the console
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
        throw;
    }
});
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}


app.Map("/error", () => Results.Problem("An error occurred."));

app.UseCors();
var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "frontend");
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(frontendPath),
    DefaultFileNames = new List<string> { "dashboard.html" }
});
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(frontendPath) });
// Serve dashboard also under /dashboard for backwards compatibility
app.MapGet("/dashboard", () => Results.File(Path.Combine(frontendPath, "dashboard.html"), "text/html"));
// Stats page
app.MapGet("/stats", () => Results.File(Path.Combine(frontendPath, "stats.html"), "text/html"));
// Fights without instance page
app.MapGet("/without", () => Results.File(Path.Combine(frontendPath, "without.html"), "text/html"));
app.MapControllers();
app.MapPost("/api/shutdown", (IHostApplicationLifetime life) =>
{
    life.StopApplication();
    return Results.Ok();
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://localhost:5005",
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to open browser");
    }
});

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

// 🔥 Sniffer w tle
Task task = Task.Run(() =>
{
    using (var initScope = scopeFactory.CreateScope())
    {
        var initContext = initScope.ServiceProvider.GetRequiredService<AppDbContext>();
        initContext.Database.EnsureCreated();
        var fightRepo = new FightRepository(initContext);
        fightRepo.AssignInstancesToExistingFightsAsync().GetAwaiter().GetResult();
    }

    var completionTracker = new InstanceCompletionTracker(scopeFactory);
    var handler = new PacketHandler();

    handler.RegisterBuffer(
        "summary",
        "3;19;",
        async (timestamp, traceId, content) =>
        {
            using var scope = scopeFactory.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var rawFightData = FightParser.ParseRawFight(timestamp, Config.PlayerName, content);
                var fight = FightParser.ToFightEntity(rawFightData);
                var repo = new FightRepository(ctx);
                await repo.AddFightAsync(fight);
                await completionTracker.ProcessFightAsync(timestamp, rawFightData.Opponents.Select(o => o.Name));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing fight packet");
            }
        });

    handler.RegisterBuffer("prices", "36;0;", (timestamp, traceId, content) =>
    {
        using var scope = scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        PricesParser.UpdateItemPrices(ctx, timestamp, traceId, content);
    });
    handler.RegisterBuffer("drif", "50;0;", (timestamp, traceId, content) =>

    {
        using var scope = scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        PricesParser.UpdateDrifPrices(ctx, timestamp, traceId, content);
    });

    handler.RegisterBuffer("instance", "1;118;", async (timestamp, traceId, content) =>
    {
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repo = new InstanceRepository(ctx);
            var instance = InstanceParser.ToInstanceEntity(timestamp, content);
            if (instance == null)
            {
                return;
            }
            var last = await repo.GetLastInstanceAsync();

            if (last != null && last.InstanceId == instance.InstanceId)
            {
                return;
            }

            await completionTracker.StartNewInstanceAsync(timestamp);
            await repo.AddInstanceAsync(instance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing instance packet");
        }
    });

    var sniffer = new PacketSniffer();
    sniffer.AddPacketHandler(handler);
    sniffer.StartCapturing();
});

// 🔥 HTTP serwer
app.Run("http://localhost:5005");
