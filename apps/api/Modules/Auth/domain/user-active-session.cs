namespace Hrm.Modules.Auth.Domain;

public sealed class UserActiveSession
{
    public long UserId { get; set; }
    public required string DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
