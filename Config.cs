namespace BrokenStatsBackend
{
    public static class Config
    {
        public static readonly string PlayerName = "Sign";
        public static readonly Dictionary<string, int> SynergeticSuffixPrices = new()
        {
            ["Duchów"] = 20250,
            ["Kłów"] = 36000,
            ["Szponów"] = 66000,
            ["Węży"] = 66000,
            ["Smoków"] = 300000,
            ["Vorlingów"] = 700000,
            ["Lodu"] = 700000,
            ["Orków"] = 2000000,
            ["Władców"] = 4500000,
            ["Torisa"] = 4500000,
            ["Medy"] = 7200000,
            ["Venegilda"] = 12750000
        };
        public const int SynergeticDefaultPrice = 11250;

        public static readonly Dictionary<string, int> TrashQualityPrices = new()
        {
            ["I"] = 0,
            ["II"] = 30,
            ["III"] = 100,
            ["IV"] = 500,
            ["V"] = 800,
            ["VI"] = 25000,
            ["VII"] = 8000,
            ["VIII"] = 25000,
            ["IX"] = 75000,
            ["X"] = 100000,
            ["XI"] = 100000,
            ["XII"] = 100000
        };
        // Price for trash with unknown quality
        public const int TrashDefaultPrice = 0;
    }
}