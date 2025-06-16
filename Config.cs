namespace BrokenStatsBackend
{
    public static class Config
    {
        public static readonly string PlayerName = "Sign";

        // Example mapping of synergetic suffix prices
        public static readonly Dictionary<string, int> SynergeticSuffixPrices = new()
        {
            ["Duchów"] = 10,
            ["Kłów"] = 15,
            ["Szponów"] = 12,
            ["Węży"] = 18,
            ["Smoków"] = 25,
            ["Vorlingów"] = 20,
            ["Lodu"] = 14,
            ["Orków"] = 16,
            ["Władców"] = 30,
            ["Torisa"] = 22,
            ["Medy"] = 17,
            ["Venegilda"] = 28
        };

        // Price for synergetics not matching any suffix above
        public const int SynergeticDefaultPrice = 5;

        // Example mapping of trash item quality (roman numerals) to price
        public static readonly Dictionary<string, int> TrashQualityPrices = new()
        {
            ["I"] = 1,
            ["II"] = 2,
            ["III"] = 3,
            ["IV"] = 4,
            ["V"] = 5,
            ["VI"] = 6,
            ["VII"] = 7,
            ["VIII"] = 8,
            ["IX"] = 9,
            ["X"] = 10,
            ["XI"] = 11,
            ["XII"] = 12
        };
        // Price for trash with unknown quality
        public const int TrashDefaultPrice = 0;
    }
}