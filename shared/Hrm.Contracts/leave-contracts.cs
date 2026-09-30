namespace Hrm.Contracts;

public static class LeaveTypes
{
    public const string Annual = "ANNUAL";
    public const string Sick = "SICK";
    public const string Maternity = "MATERNITY";
    public const string Resignation = "RESIGNATION";

    public static string GetDisplayName(string type) => type?.ToUpperInvariant() switch
    {
        Annual => "Nghỉ phép năm",
        Sick => "Nghỉ ốm đau",
        Maternity => "Nghỉ thai sản",
        Resignation => "Nghỉ việc / Thôi việc",
        _ => type ?? "Nghỉ phép"
    };
}

public static class LeaveStatuses
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";

    public static string GetDisplayName(string status) => status switch
    {
        Pending => "Chờ duyệt",
        Approved => "Đã duyệt",
        Rejected => "Bị từ chối",
        Cancelled => "Đã hủy",
        _ => status
    };
}

public sealed record CreateLeaveRequestDto(
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    Guid? AttachmentFileId = null,
    string? AttachmentFileName = null
);

public sealed record UpdateLeaveRequestDto(
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    Guid? AttachmentFileId = null,
    string? AttachmentFileName = null
);

public sealed record LeaveRequestDto(
    Guid Id,
    long EmployeeId,
    string LeaveType,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DaysCount,
    string Reason,
    string Status,
    string StatusName,
    string? RejectionReason,
    Guid? AttachmentFileId,
    string? AttachmentFileName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public sealed record LeaveBalanceSummaryDto(
    decimal TotalAnnualEntitlement,
    decimal UsedAnnualDays,
    decimal RemainingAnnualDays,
    int PendingRequestsCount
);

public sealed record PendingLeaveApprovalDto(
    Guid Id,
    long EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string LeaveType,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DaysCount,
    string Reason,
    string Status,
    string StatusName,
    Guid? AttachmentFileId,
    string? AttachmentFileName,
    DateTimeOffset CreatedAt
);

public sealed record RejectLeaveRequestInput(string? Reason = null);

