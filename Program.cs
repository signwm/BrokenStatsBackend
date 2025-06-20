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
    var fightRepository = new FightRepository(context);
    var instanceRepository = new InstanceRepository(context);
    var handler = new PacketHandler();

    handler.RegisterBuffer(
        "summary",
        "3;19;",
        (timestamp, traceId, content) =>
        {
            try
            {
                var rawFightData = FightParser.ParseRawFight(timestamp, Config.PlayerName, content);
                var fight = FightParser.ToFightEntity(rawFightData);
                _ = fightRepository.AddFightAsync(fight);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });

    handler.RegisterBuffer("prices", "36;0;", (timestamp, traceId, content) =>
    {
        PricesParser.UpdateItemPrices(context, timestamp, traceId, content);
    });
    handler.RegisterBuffer("drif", "50;0;", (timestamp, traceId, content) =>

    {
        PricesParser.UpdateDrifPrices(context, timestamp, traceId, content);
    });

    handler.RegisterBuffer("instance", "1;118;", (timestamp, traceId, content) =>
    {
        try
        {
            var instance = InstanceParser.ToInstanceEntity(timestamp, content);
            _ = instanceRepository.AddInstanceAsync(instance);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    });

    var sniffer = new PacketSniffer();
    sniffer.AddPacketHandler(handler);
    sniffer.StartCapturing();
});

// 🔥 HTTP serwer
app.Run("http://localhost:5005");
