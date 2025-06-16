using BrokenStatsBackend;
using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Repositories;
using BrokenStatsBackend.src.Parser;
using Microsoft.EntityFrameworkCore;
using BrokenStatsBackend.src.Network;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

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

app.UseCors();
var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "frontend");
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = new PhysicalFileProvider(frontendPath) });
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(frontendPath) });
app.MapControllers();

// 🔥 Sniffer w tle
Task task = Task.Run(() =>
{
    var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
    var repository = new FightRepository(context);
    var handler = new PacketHandler();

    handler.RegisterBuffer(new PacketBuffer(
        "summary",
        "33-3b-31-39-3b",      // start 3;19;
        (timestamp, traceId, content) =>
        {
            try
            {
                var rawFightData = FightParser.ParseRawFight(timestamp, Config.PlayerName, content);
                var fight = FightParser.ToFightEntity(rawFightData);
                _ = repository.AddFightAsync(fight);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }));

    handler.RegisterBuffer(new PacketBuffer("prices", "33-36-3b-30-3b", (timestamp, traceId, content) =>
    {
        PricesParser.UpdateItemPrices(context, timestamp, traceId, content);
    }));
    handler.RegisterBuffer(new PacketBuffer("drif", "35-30-3b-30-3b", (timestamp, traceId, content) =>
    {
        PricesParser.UpdateDrifPrices(context, timestamp, traceId, content);
    }));

    var sniffer = new PacketSniffer();
    sniffer.AddPacketHandler(handler);
    sniffer.StartCapturing();
});

// 🔥 HTTP serwer
app.Run("http://localhost:5005");
