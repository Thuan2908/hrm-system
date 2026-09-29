namespace Hrm.Contracts;

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
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason
);

public sealed record LeaveRequestDto(
    Guid Id,
    long EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DaysCount,
    string Reason,
    string Status,
    string StatusName,
    string? RejectionReason,
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
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DaysCount,
    string Reason,
    string Status,
    string StatusName,
    DateTimeOffset CreatedAt
);

public sealed record RejectLeaveRequestInput(string? Reason = null);

