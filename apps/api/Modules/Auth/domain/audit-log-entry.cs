namespace Hrm.Modules.Auth.Domain;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public long? ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
