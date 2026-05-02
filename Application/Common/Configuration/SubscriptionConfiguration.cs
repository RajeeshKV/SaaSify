namespace Application.Common.Configuration;

public class SubscriptionConfiguration
{
    public List<PlanConfiguration> Plans { get; set; } = new();
}

public class PlanConfiguration
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal MonthlyPrice { get; set; }
    public int RateLimitPerMinute { get; set; }
    public int MaxUsers { get; set; }
    public List<string> Features { get; set; } = new();
}
