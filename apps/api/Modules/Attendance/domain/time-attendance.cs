namespace Hrm.Modules.Attendance.Domain;

public sealed class TimeAttendance
{
    public Guid Id { get; set; }
    public long EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTimeOffset CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public decimal? ActualHours { get; set; }
    public string Status { get; set; } = "PRESENT";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
