namespace Domain.Entities;

public class User
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public int TokenVersion { get; set; } = 0;
    public bool IsEmailVerified { get; set; } = false;
    public string EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpires { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    public Tenant Tenant { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
