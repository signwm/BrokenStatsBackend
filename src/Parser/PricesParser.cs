using BrokenStatsBackend.Logging;
using BrokenStatsBackend.src.Database;
using BrokenStatsBackend.src.Models;

namespace BrokenStatsBackend.src.Parser;

public class PricesParser
{
    public static void UpdateItemPrices(AppDbContext db, DateTime timestamp, string traceId, string rawInput)
    {
        try
        {
            var rows = rawInput.Split("[&&]");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var parts = row.Split(',');
                if (parts.Length >= 3)
                {
                    string name = parts[1].Trim();
                    if (seen.Contains(name)) continue;

                    if (int.TryParse(parts[2], out int price))
                    {
                        UpsertDropItemPrice(db, name, price, dropTypeId: 2); // 🟢 ITEM
                        seen.Add(name);
                    }
                }
            }

            db.SaveChanges();
            Logger.Log(traceId, rawInput, "ITEMS_UPDATED");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Błąd w UpdateItemPrices: " + ex);
        }
    }


    public static void UpdateDrifPrices(AppDbContext db, DateTime timestamp, string traceId, string rawInput)
    {
        try
        {
            var rows = rawInput.Split("[&&]");

            foreach (var row in rows)
            {
                var parts = row.Split(',');
                if (parts.Length >= 5)
                {
                    string name = parts[4].Trim();
                    if (int.TryParse(parts[1], out int price))
                    {
                        UpsertDropItemPrice(db, name, price, dropTypeId: 5); // 🟣 DRIF
                    }
                }
            }

            db.SaveChanges();
            Logger.Log(traceId, rawInput, "DRIFS_UPDATED");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Błąd w UpdateDrifPrices: " + ex);
        }
    }


    private static void UpsertDropItemPrice(AppDbContext db, string name, int value, int dropTypeId)
    {
        var item = db.DropItems.FirstOrDefault(i => i.Name == name);

        if (item == null)
        {
            db.DropItems.Add(new DropItemEntity
            {
                Name = name,
                Value = value,
                Quality = null,
                DropTypeId = dropTypeId
            });
        }
        else if (item.Value != value)
        {
            item.Value = value;
            db.DropItems.Update(item);
        }
    }

}
