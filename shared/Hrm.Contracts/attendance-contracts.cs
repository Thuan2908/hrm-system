namespace Hrm.Contracts;

public sealed record AttendanceTodayResponse(
    bool HasCheckedIn,
    bool HasCheckedOut,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    decimal? ActualHours,
    string Status,
    string? Message
);

public sealed record AttendanceRecordDto(
    Guid Id,
    DateOnly WorkDate,
    DateTimeOffset CheckInTime,
    DateTimeOffset? CheckOutTime,
    decimal? ActualHours,
    string Status,
    string? Notes
);

public sealed record CheckInRequest(string? Notes = null);

public sealed record CheckOutRequest(string? Notes = null);

public sealed record CheckInResultDto(
    Guid Id,
    DateTimeOffset CheckInTime,
    string Status,
    string Message
);

public sealed record CheckOutResultDto(
    Guid Id,
    DateTimeOffset CheckInTime,
    DateTimeOffset CheckOutTime,
    decimal ActualHours,
    string Status,
    string Message
);
