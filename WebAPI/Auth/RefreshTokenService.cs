using System.Security.Cryptography;
using System.Text;

public static class RefreshTokenService
{
    private const int RefreshTokenBytes = 64;

    public static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
    }

    public static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }
}
