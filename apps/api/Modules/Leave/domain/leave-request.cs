namespace Hrm.Modules.Leave.Domain;

public sealed class LeaveRequest
{
    public Guid Id { get; set; }
    public long EmployeeId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysCount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? RejectionReason { get; set; }
    public Guid? AttachmentFileId { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
