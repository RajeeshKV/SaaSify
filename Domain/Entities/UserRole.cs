namespace Domain.Entities;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int TenantId { get; set; }
    public DateTime AssignedAt { get; set; }

    public User User { get; set; }
    public Role Role { get; set; }
}
