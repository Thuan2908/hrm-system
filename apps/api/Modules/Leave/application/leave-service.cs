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
    Task<LeaveRequestDto> UpdateLeaveRequestAsync(long userId, Guid requestId, UpdateLeaveRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(long userId, CancellationToken cancellationToken);
    Task CancelLeaveRequestAsync(long userId, Guid requestId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PendingLeaveApprovalDto>> GetPendingRequestsAsync(CancellationToken cancellationToken);
    Task ApproveLeaveRequestAsync(Guid requestId, long actorUserId, CancellationToken cancellationToken);
    Task RejectLeaveRequestAsync(Guid requestId, string? reason, long actorUserId, CancellationToken cancellationToken);
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
                     && r.LeaveType == LeaveTypes.Annual
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
            throw new DomainException("REASON_REQUIRED", "Vui lòng nhập lý do xin nghỉ.");
        }

        var leaveType = string.IsNullOrWhiteSpace(request.LeaveType)
            ? LeaveTypes.Annual
            : request.LeaveType.Trim().ToUpperInvariant();

        // Kiểm tra trùng lặp với đơn đang chờ hoặc đã duyệt
        var isOverlapping = await dbContext.LeaveRequests.AsNoTracking()
            .AnyAsync(r => r.EmployeeId == employeeId
                        && (r.Status == LeaveStatuses.Pending || r.Status == LeaveStatuses.Approved)
                        && !(request.EndDate < r.StartDate || request.StartDate > r.EndDate),
                      cancellationToken);

        if (isOverlapping)
        {
            throw new DomainException("LEAVE_OVERLAP", "Bạn đã có đơn nghỉ khác trong khoảng thời gian này.");
        }

        // Tính số ngày làm việc
        var daysCount = leaveType == LeaveTypes.Resignation
            ? 1m
            : CalculateWorkingDays(request.StartDate, request.EndDate);

        if (daysCount <= 0)
        {
            throw new DomainException("INVALID_DAYS", "Khoảng thời gian đã chọn không có ngày làm việc hợp lệ.");
        }

        var now = timeProvider.GetUtcNow();
        var entity = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DaysCount = daysCount,
            Reason = request.Reason.Trim(),
            Status = LeaveStatuses.Pending,
            AttachmentFileId = request.AttachmentFileId,
            AttachmentFileName = request.AttachmentFileName,
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

    public async Task<LeaveRequestDto> UpdateLeaveRequestAsync(
        long userId,
        Guid requestId,
        UpdateLeaveRequestDto request,
        CancellationToken cancellationToken)
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
            throw new DomainException("LEAVE_CANNOT_EDIT", "Chỉ có thể chỉnh sửa đơn đang ở trạng thái Chờ duyệt.");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new DomainException("INVALID_DATE_RANGE", "Ngày kết thúc không được trước ngày bắt đầu nghỉ.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new DomainException("REASON_REQUIRED", "Vui lòng nhập lý do xin nghỉ.");
        }

        var leaveType = string.IsNullOrWhiteSpace(request.LeaveType)
            ? LeaveTypes.Annual
            : request.LeaveType.Trim().ToUpperInvariant();

        // Kiểm tra trùng lặp với đơn khác (loại trừ chính đơn này)
        var isOverlapping = await dbContext.LeaveRequests.AsNoTracking()
            .AnyAsync(r => r.EmployeeId == employeeId
                        && r.Id != requestId
                        && (r.Status == LeaveStatuses.Pending || r.Status == LeaveStatuses.Approved)
                        && !(request.EndDate < r.StartDate || request.StartDate > r.EndDate),
                      cancellationToken);

        if (isOverlapping)
        {
            throw new DomainException("LEAVE_OVERLAP", "Bạn đã có đơn nghỉ khác trong khoảng thời gian này.");
        }

        var daysCount = leaveType == LeaveTypes.Resignation
            ? 1m
            : CalculateWorkingDays(request.StartDate, request.EndDate);

        if (daysCount <= 0)
        {
            throw new DomainException("INVALID_DAYS", "Khoảng thời gian đã chọn không có ngày làm việc hợp lệ.");
        }

        entity.LeaveType = leaveType;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.DaysCount = daysCount;
        entity.Reason = request.Reason.Trim();
        if (request.AttachmentFileId.HasValue)
        {
            entity.AttachmentFileId = request.AttachmentFileId;
            entity.AttachmentFileName = request.AttachmentFileName;
        }
        entity.UpdatedAt = timeProvider.GetUtcNow();

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
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
        LeaveType: e.LeaveType,
        LeaveTypeName: LeaveTypes.GetDisplayName(e.LeaveType),
        StartDate: e.StartDate,
        EndDate: e.EndDate,
        DaysCount: e.DaysCount,
        Reason: e.Reason,
        Status: e.Status,
        StatusName: LeaveStatuses.GetDisplayName(e.Status),
        RejectionReason: e.RejectionReason,
        AttachmentFileId: e.AttachmentFileId,
        AttachmentFileName: e.AttachmentFileName,
        CreatedAt: e.CreatedAt,
        UpdatedAt: e.UpdatedAt
    );

    public async Task<IReadOnlyList<PendingLeaveApprovalDto>> GetPendingRequestsAsync(CancellationToken cancellationToken)
    {
        var pending = await dbContext.LeaveRequests.AsNoTracking()
            .Where(r => r.Status == LeaveStatuses.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var employeeIds = pending.Select(p => p.EmployeeId).Distinct().ToList();
        var employees = await authDbContext.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        return pending.Select(r =>
        {
            employees.TryGetValue(r.EmployeeId, out var emp);
            return new PendingLeaveApprovalDto(
                r.Id,
                r.EmployeeId,
                emp?.Code ?? "--",
                emp?.FullName ?? "Nhân viên #" + r.EmployeeId,
                emp?.Department?.Name ?? "N/A",
                r.LeaveType,
                LeaveTypes.GetDisplayName(r.LeaveType),
                r.StartDate,
                r.EndDate,
                r.DaysCount,
                r.Reason,
                r.Status,
                LeaveStatuses.GetDisplayName(r.Status),
                r.AttachmentFileId,
                r.AttachmentFileName,
                r.CreatedAt
            );
        }).ToList();
    }

    public async Task ApproveLeaveRequestAsync(Guid requestId, long actorUserId, CancellationToken cancellationToken)
    {
        var request = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new DomainException("LEAVE_REQUEST_NOT_FOUND", "Không tìm thấy đơn xin nghỉ phép.");

        if (request.Status != LeaveStatuses.Pending)
        {
            throw new DomainException("LEAVE_ALREADY_PROCESSED", "Đơn xin nghỉ phép đã được xử lý trước đó.");
        }

        if (actorUserId > 0)
        {
            var actorEmployeeId = await GetEmployeeIdAsync(actorUserId, cancellationToken);
            if (request.EmployeeId == actorEmployeeId)
            {
                throw new DomainException("CANNOT_APPROVE_SELF", "Bạn không được tự phê duyệt đơn của chính mình.");
            }
        }

        var now = timeProvider.GetUtcNow();
        request.Status = LeaveStatuses.Approved;
        request.ApprovedByUserId = actorUserId;
        request.ApprovedAt = now;
        request.UpdatedAt = now;

        if (string.Equals(request.LeaveType, LeaveTypes.Resignation, StringComparison.OrdinalIgnoreCase))
        {
            var emp = await authDbContext.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);
            if (emp is not null)
            {
                emp.Status = "RESIGNED";
                await authDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectLeaveRequestAsync(Guid requestId, string? reason, long actorUserId, CancellationToken cancellationToken)
    {
        var request = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new DomainException("LEAVE_REQUEST_NOT_FOUND", "Không tìm thấy đơn xin nghỉ phép.");

        if (request.Status != LeaveStatuses.Pending)
        {
            throw new DomainException("LEAVE_ALREADY_PROCESSED", "Đơn xin nghỉ phép đã được xử lý trước đó.");
        }

        var now = timeProvider.GetUtcNow();
        request.Status = LeaveStatuses.Rejected;
        request.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Quản lý từ chối" : reason.Trim();
        request.ApprovedByUserId = actorUserId;
        request.ApprovedAt = now;
        request.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
