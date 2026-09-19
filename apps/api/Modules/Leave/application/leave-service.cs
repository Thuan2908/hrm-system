using Hrm.Contracts;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.Modules.Leave.Domain;
using Hrm.Modules.Leave.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Leave.Application;

public interface ILeaveService
{
    Task<LeaveBalanceSummaryDto> GetLeaveBalanceAsync(long userId, CancellationToken cancellationToken);
    Task<LeaveRequestDto> CreateLeaveRequestAsync(long userId, CreateLeaveRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(long userId, CancellationToken cancellationToken);
    Task CancelLeaveRequestAsync(long userId, Guid requestId, CancellationToken cancellationToken);
}

public sealed class LeaveService(
    LeaveDbContext dbContext,
    AuthDbContext authDbContext,
    TimeProvider timeProvider) : ILeaveService
{
    private const decimal StandardAnnualLeaveDays = 12.0m;

    public async Task<LeaveBalanceSummaryDto> GetLeaveBalanceAsync(long userId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        var currentYear = timeProvider.GetUtcNow().Year;

        var approvedAnnualDays = await dbContext.LeaveRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId
                     && r.Status == LeaveStatuses.Approved
                     && r.StartDate.Year == currentYear)
            .SumAsync(r => (decimal?)r.DaysCount, cancellationToken) ?? 0m;

        var pendingCount = await dbContext.LeaveRequests.AsNoTracking()
            .CountAsync(r => r.EmployeeId == employeeId && r.Status == LeaveStatuses.Pending, cancellationToken);

        var remainingDays = Math.Max(0m, StandardAnnualLeaveDays - approvedAnnualDays);

        return new LeaveBalanceSummaryDto(
            TotalAnnualEntitlement: StandardAnnualLeaveDays,
            UsedAnnualDays: approvedAnnualDays,
            RemainingAnnualDays: remainingDays,
            PendingRequestsCount: pendingCount
        );
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(
        long userId,
        CreateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);

        if (request.EndDate < request.StartDate)
        {
            throw new DomainException("INVALID_DATE_RANGE", "Ngày kết thúc không được trước ngày bắt đầu nghỉ.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new DomainException("REASON_REQUIRED", "Vui lòng nhập lý do xin nghỉ phép.");
        }

        // Kiểm tra trùng lặp với đơn đang chờ hoặc đã duyệt
        var isOverlapping = await dbContext.LeaveRequests.AsNoTracking()
            .AnyAsync(r => r.EmployeeId == employeeId
                        && (r.Status == LeaveStatuses.Pending || r.Status == LeaveStatuses.Approved)
                        && !(request.EndDate < r.StartDate || request.StartDate > r.EndDate),
                      cancellationToken);

        if (isOverlapping)
        {
            throw new DomainException("LEAVE_OVERLAP", "Bạn đã có đơn nghỉ phép khác trong khoảng thời gian này.");
        }

        // Tính số ngày làm việc (không tính Chủ Nhật)
        var daysCount = CalculateWorkingDays(request.StartDate, request.EndDate);
        if (daysCount <= 0)
        {
            throw new DomainException("INVALID_DAYS", "Khoảng thời gian đã chọn không có ngày làm việc hợp lệ.");
        }

        var now = timeProvider.GetUtcNow();
        var entity = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveType = "ANNUAL",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DaysCount = daysCount,
            Reason = request.Reason.Trim(),
            Status = LeaveStatuses.Pending,
            CreatedAt = now
        };

        dbContext.LeaveRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(long userId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);

        var list = await dbContext.LeaveRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task CancelLeaveRequestAsync(long userId, Guid requestId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);

        var entity = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.EmployeeId == employeeId, cancellationToken);

        if (entity is null)
        {
            throw new DomainException("LEAVE_NOT_FOUND", "Không tìm thấy đơn xin nghỉ phép.");
        }

        if (entity.Status != LeaveStatuses.Pending)
        {
            throw new DomainException("CANNOT_CANCEL", "Chỉ có thể hủy đơn xin nghỉ phép khi đơn đang ở trạng thái Chờ duyệt.");
        }

        entity.Status = LeaveStatuses.Cancelled;
        entity.UpdatedAt = timeProvider.GetUtcNow();

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static decimal CalculateWorkingDays(DateOnly start, DateOnly end)
    {
        decimal days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            // Bỏ qua Chủ Nhật
            if (date.DayOfWeek != DayOfWeek.Sunday)
            {
                days += 1.0m;
            }
        }
        return days;
    }

    private static LeaveRequestDto MapToDto(LeaveRequest e) => new(
        Id: e.Id,
        EmployeeId: e.EmployeeId,
        StartDate: e.StartDate,
        EndDate: e.EndDate,
        DaysCount: e.DaysCount,
        Reason: e.Reason,
        Status: e.Status,
        StatusName: LeaveStatuses.GetDisplayName(e.Status),
        RejectionReason: e.RejectionReason,
        CreatedAt: e.CreatedAt,
        UpdatedAt: e.UpdatedAt
    );
}
