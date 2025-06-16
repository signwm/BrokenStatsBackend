using System.Text.RegularExpressions;
using BrokenStatsBackend.src.Models;
using BrokenStatsBackend.src.Repositories;

namespace BrokenStatsBackend.src.Parser
{
    public static class FightParser
    {
        private static readonly List<string> Tags =
        [
            "[s8]", "[/s8]", "[rr]", "[/rr]", "[s0]", "[/s0]",
            "[s4]", "[/s4]", "[r2]", "[/r2]", "[r1]", "[/r1]",
            "[r3]", "[/r3]", "[r4]", "[/r4]", "[r5]", "[/r5]"
        ];

        public class RawFightData
        {
            public DateTime Time { get; set; }
            public int Exp { get; set; }
            public int Gold { get; set; }
            public int Psycho { get; set; }
            public string RawRaresAndSyngs { get; set; } = string.Empty;
            public string RawItems { get; set; } = string.Empty;
            public string RawDrifs { get; set; } = string.Empty;
            public string RawTrash { get; set; } = string.Empty;
            public List<(string Name, int Level)> Opponents { get; set; } = new();
        }

        public static RawFightData ParseRawFight(DateTime timestamp, string playerName, string rawData)
        {
            var split = rawData.Split('$');
            var result = new RawFightData { Time = timestamp };
            bool playerDataAdded = false;

            foreach (var playerString in split)
            {
                var playerData = playerString.Split('&');

                if (!playerDataAdded && playerData[1] == playerName)
                {
                    playerDataAdded = true;
                    result.Exp = int.Parse(playerData[2]);
                    result.Gold = int.Parse(playerData[4]);
                    result.Psycho = int.Parse(playerData[24]);

                    result.RawRaresAndSyngs = playerData[7];
                    result.RawItems = playerData[9];
                    result.RawDrifs = playerData.Length > 25 ? playerData[25] : string.Empty;
                    result.RawTrash = playerData.Length > 27 ? playerData[27] : string.Empty;
                }
                else if (playerData[0] == "2")
                {
                    string name = playerData[1].Trim();
                    int level = int.Parse(playerData[15]);
                    result.Opponents.Add((name, level));
                }
            }

            return result;
        }

        public static FightEntity ToFightEntity(RawFightData raw, FightRepository repository)
        {
            var fight = new FightEntity
            {
                PublicId = Guid.NewGuid(),
                Time = raw.Time,
                Exp = raw.Exp,
                Gold = raw.Gold,
                Psycho = raw.Psycho,
                Opponents = new List<OpponentEntity>(),
                Drops = new List<DropEntity>()
            };

            foreach (var (name, level) in raw.Opponents)
            {
                var opponentType = repository.GetOrCreateOpponentType(name, level);
                fight.Opponents.Add(new OpponentEntity { OpponentType = opponentType });
            }

            string rares = ExtractRares(RemoveTags(raw.RawRaresAndSyngs));
            string syngs = ExtractSyngs(RemoveTags(raw.RawRaresAndSyngs));

            fight.Drops.AddRange(ParseDrops(rares, "Rare", fight, repository));
            fight.Drops.AddRange(ParseDrops(syngs, "Synergetic", fight, repository));
            fight.Drops.AddRange(ParseDrops(RemoveTags(raw.RawItems), "Item", fight, repository));
            fight.Drops.AddRange(ParseDrops(RemoveTags(raw.RawDrifs), "Drif", fight, repository));
            fight.Drops.AddRange(ParseDrops(RemoveTags(raw.RawTrash), "Trash", fight, repository));

            return fight;
        }

        private static List<DropEntity> ParseDrops(string raw, string dropTypeStr, FightEntity fight, FightRepository repository)
        {
            if (string.IsNullOrWhiteSpace(raw)) return [];


            var items = raw
                .Trim()
                .Split(new[] { "  ", "\t", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();


            var result = new List<DropEntity>();

            foreach (var item in items)
            {
                int quantity = 1;
                string text = item;

                var quantityMatch = Regex.Match(text, @"\((\d+)\)$");
                if (quantityMatch.Success)
                {
                    quantity = int.Parse(quantityMatch.Groups[1].Value);
                    text = text[..text.LastIndexOf('(')].Trim();
                }

                string? quality = null;
                var qualityMatch = Regex.Match(text, @"\[([^\]]+)\]$");
                if (qualityMatch.Success)
                {
                    quality = qualityMatch.Groups[1].Value;
                    text = text[..text.LastIndexOf('[')].Trim();
                }

                var dropItem = repository.GetOrCreateDropItem(text, quality, dropTypeStr);

                result.Add(new DropEntity
                {
                    Fight = fight,
                    DropItem = dropItem,
                    Quantity = quantity
                });
            }

            return result;
        }


        private static string RemoveTags(string s)
        {
            foreach (var tag in Tags)
                s = s.Replace(tag, "");
            return s;
        }

        private static string ExtractRares(string s)
        {

            var regex = "\"([^\"]+)\"\\[[^\\]]+\\]";
            var matches = Regex.Matches(s, regex);
            return string.Join(" ", matches.Select(m => m.Groups[1].Value));
        }

        private static string ExtractSyngs(string s)
        {
            var regex = "([A-Za-ząęłśżźóćńĄĘŁŚŻŹÓĆŃ]+(?: [A-Za-ząęłśżźóćńĄĘŁŚŻŹÓĆŃ]+)*\\[.*?\\])";
            var matches = Regex.Matches(s, regex);
            return string.Join(" ", matches.Select(m => m.Groups[1].Value));
        }
    }
}
