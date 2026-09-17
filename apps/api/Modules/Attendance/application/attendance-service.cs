using Hrm.Contracts;
using Hrm.Modules.Attendance.Domain;
using Hrm.Modules.Attendance.Infrastructure.Persistence;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Attendance.Application;

public interface IAttendanceService
{
    Task<AttendanceTodayResponse> GetTodayStatusAsync(long userId, CancellationToken cancellationToken);
    Task<CheckInResultDto> CheckInAsync(long userId, CheckInRequest request, CancellationToken cancellationToken);
    Task<CheckOutResultDto> CheckOutAsync(long userId, CheckOutRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttendanceRecordDto>> GetMyHistoryAsync(long userId, int days, CancellationToken cancellationToken);
}

public sealed class AttendanceService(
    AttendanceDbContext dbContext,
    AuthDbContext authDbContext,
    TimeProvider timeProvider) : IAttendanceService
{
    public async Task<AttendanceTodayResponse> GetTodayStatusAsync(long userId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        var today = GetCurrentWorkDate();

        var record = await dbContext.TimeAttendances.AsNoTracking()
            .FirstOrDefaultAsync(item => item.EmployeeId == employeeId && item.WorkDate == today, cancellationToken);

        if (record is null)
        {
            return new AttendanceTodayResponse(
                HasCheckedIn: false,
                HasCheckedOut: false,
                CheckInTime: null,
                CheckOutTime: null,
                ActualHours: null,
                Status: "NOT_CHECKED_IN",
                Message: "Hôm nay bạn chưa điểm danh vào ca.");
        }

        if (record.CheckOutTime is null)
        {
            return new AttendanceTodayResponse(
                HasCheckedIn: true,
                HasCheckedOut: false,
                CheckInTime: record.CheckInTime,
                CheckOutTime: null,
                ActualHours: null,
                Status: record.Status,
                Message: "Đang trong ca làm việc.");
        }

        return new AttendanceTodayResponse(
            HasCheckedIn: true,
            HasCheckedOut: true,
            CheckInTime: record.CheckInTime,
            CheckOutTime: record.CheckOutTime,
            ActualHours: record.ActualHours,
            Status: record.Status,
            Message: "Bạn đã hoàn thành ca làm việc hôm nay.");
    }

    public async Task<CheckInResultDto> CheckInAsync(long userId, CheckInRequest request, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        var today = GetCurrentWorkDate();

        var existingRecord = await dbContext.TimeAttendances
            .FirstOrDefaultAsync(item => item.EmployeeId == employeeId && item.WorkDate == today, cancellationToken);

        if (existingRecord is not null)
        {
            throw new DomainException("ATTENDANCE_ALREADY_CHECKED_IN", "Bạn đã điểm danh vào ca hôm nay rồi.");
        }

        var now = timeProvider.GetUtcNow();
        var newRecord = new TimeAttendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = today,
            CheckInTime = now,
            Status = "WORKING",
            Notes = request.Notes,
            CreatedAt = now
        };

        dbContext.TimeAttendances.Add(newRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CheckInResultDto(
            Id: newRecord.Id,
            CheckInTime: newRecord.CheckInTime,
            Status: newRecord.Status,
            Message: "Điểm danh vào ca thành công!");
    }

    public async Task<CheckOutResultDto> CheckOutAsync(long userId, CheckOutRequest request, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        var today = GetCurrentWorkDate();

        var record = await dbContext.TimeAttendances
            .FirstOrDefaultAsync(item => item.EmployeeId == employeeId && item.WorkDate == today, cancellationToken);

        if (record is null)
        {
            throw new DomainException("ATTENDANCE_NOT_CHECKED_IN", "Bạn chưa điểm danh vào ca hôm nay, không thể check-out.");
        }

        if (record.CheckOutTime is not null)
        {
            throw new DomainException("ATTENDANCE_ALREADY_CHECKED_OUT", "Bạn đã điểm danh ra ca hôm nay rồi.");
        }

        var now = timeProvider.GetUtcNow();
        var duration = now - record.CheckInTime;
        var hours = Math.Round((decimal)Math.Max(0.01, duration.TotalHours), 2);

        record.CheckOutTime = now;
        record.ActualHours = hours;
        record.Status = "COMPLETED";
        record.UpdatedAt = now;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            record.Notes = string.IsNullOrWhiteSpace(record.Notes)
                ? request.Notes
                : $"{record.Notes}; {request.Notes}";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CheckOutResultDto(
            Id: record.Id,
            CheckInTime: record.CheckInTime,
            CheckOutTime: now,
            ActualHours: hours,
            Status: record.Status,
            Message: "Điểm danh ra ca thành công. Chúc bạn một ngày tốt lành!");
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetMyHistoryAsync(long userId, int days, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        var limitDays = Math.Clamp(days, 1, 60);
        var startDate = GetCurrentWorkDate().AddDays(-limitDays);

        var records = await dbContext.TimeAttendances.AsNoTracking()
            .Where(item => item.EmployeeId == employeeId && item.WorkDate >= startDate)
            .OrderByDescending(item => item.WorkDate)
            .ThenByDescending(item => item.CheckInTime)
            .Select(item => new AttendanceRecordDto(
                item.Id,
                item.WorkDate,
                item.CheckInTime,
                item.CheckOutTime,
                item.ActualHours,
                item.Status,
                item.Notes))
            .ToListAsync(cancellationToken);

        return records;
    }

    private async Task<long> GetEmployeeIdAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await authDbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new DomainException("AUTH_USER_NOT_FOUND", "Không tìm thấy thông tin tài khoản.");
        }

        return user.EmployeeId;
    }

    private DateOnly GetCurrentWorkDate()
    {
        // Chuyển sang giờ Việt Nam (UTC+7) để tính chuẩn work_date
        var vnTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            timeProvider.GetUtcNow().UtcDateTime, "SE Asia Standard Time");
        return DateOnly.FromDateTime(vnTime);
    }
}
