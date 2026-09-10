namespace EvolFit.Core.Entities;

/// <summary>
/// Refresh token persistido. O valor em claro NUNCA é armazenado —
/// guardamos apenas o hash SHA-256 (SEC-002).
/// </summary>
public class RefreshToken
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public int? ReplacedById { get; private set; }

    // Navegação
    public User? User { get; private set; }
    public RefreshToken? ReplacedBy { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(int userId, string tokenHash, DateTime expiresAt)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public void Revoke(int? replacedById = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedById = replacedById;
    }
}
