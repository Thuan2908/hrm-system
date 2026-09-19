namespace Hrm.Modules.Employees.Domain;

public sealed class EmployeeTimelineEvent
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public required string EventType { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public long? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
}
