namespace Domain.Entities;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Plan { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}