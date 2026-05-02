namespace Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; }
    public string Entity { get; set; }
    public string EntityId { get; set; }
    public string Data { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }

    public Tenant Tenant { get; set; }
    public User User { get; set; }
}
