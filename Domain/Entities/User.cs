namespace Domain.Entities;

public class User
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public int TokenVersion { get; set; } = 0;

    public Tenant Tenant { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
