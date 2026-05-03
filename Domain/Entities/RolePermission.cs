namespace Domain.Entities;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public int TenantId { get; set; }
    public DateTime GrantedAt { get; set; }

    public Role Role { get; set; }
    public Permission Permission { get; set; }
}
