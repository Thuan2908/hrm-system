namespace Hrm.Modules.Auth.Domain;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public long UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
