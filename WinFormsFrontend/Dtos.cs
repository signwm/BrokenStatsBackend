namespace BrokenStatsFrontendWinForms;

public class InstanceDto
{
    public int id { get; set; }
    public DateTime startTime { get; set; }
    public string name { get; set; } = string.Empty;
    public int difficulty { get; set; }
    public int gold { get; set; }
    public int exp { get; set; }
    public int psycho { get; set; }
    public int profit { get; set; }
    public int fights { get; set; }
    public int? durationSeconds { get; set; }
}

public class InstanceFightDto
{
    public Guid id { get; set; }
    public int offsetSeconds { get; set; }
    public int exp { get; set; }
    public int gold { get; set; }
    public int psycho { get; set; }
    public int dropValue { get; set; }
    public string opponents { get; set; } = string.Empty;
    public string drops { get; set; } = string.Empty;
}
