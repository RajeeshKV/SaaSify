namespace Application.Common.Configuration;

public class RBACMigrationConfiguration
{
    public List<DefaultRole> DefaultRoles { get; set; } = new();
    public List<DefaultPermission> DefaultPermissions { get; set; } = new();
    public List<RolePermissionMapping> RolePermissionMappings { get; set; } = new();
    public List<UserRoleMapping> UserRoleMappings { get; set; } = new();
}

public class DefaultRole
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsDefault { get; set; } = false; // Fallback role for users without specific assignments
}

public class DefaultPermission
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
}

public class RolePermissionMapping
{
    public string Role { get; set; } = "";
    public List<string> Permissions { get; set; } = new();
}

public class UserRoleMapping
{
    public string UserRole { get; set; } = ""; // Maps to existing user.role field
    public string AssignedRole { get; set; } = ""; // Maps to new RBAC role
    public int Priority { get; set; } = 0; // Higher priority takes precedence
}
