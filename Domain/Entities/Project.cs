namespace Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public string Name { get; set; }

    public Tenant Tenant { get; set; }
}