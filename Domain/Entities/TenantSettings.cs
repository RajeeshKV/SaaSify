namespace Domain.Entities;

public class TenantSettings
{
    public int TenantId { get; set; }
    public int MaxProjects { get; set; }
    public int MaxUsers { get; set; }
    public bool EnableAdvancedFeatures { get; set; }
    public bool EnableApiAccess { get; set; }
    public bool EnableExport { get; set; }
    public bool EnableIntegrations { get; set; }
    public long MaxStorageMB { get; set; }
    public int MaxApiCallsPerDay { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; }
}
