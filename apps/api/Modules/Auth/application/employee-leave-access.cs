using System.Text.Json;
using Hrm.Modules.Auth.Contracts;
using Hrm.Modules.Auth.Domain;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Auth.Application;

public sealed class EmployeeLeaveAccess(AuthDbContext dbContext, TimeProvider timeProvider) : IEmployeeLeaveAccess
{
    public async Task<LeaveUserAccess> GetUserAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().Include(u => u.Employee).Include(u => u.SecurityState)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new DomainException("AUTH_USER_NOT_FOUND", "Không tìm thấy tài khoản.");
        var permissions = await dbContext.RolePermissions.AsNoTracking()
            .Where(g => g.RoleId == user.RoleId).Select(g => g.Permission.Code).ToListAsync(cancellationToken);
        var active = user.IsActive && (user.Employee == null || user.Employee.Status == "ACTIVE")
            && (user.SecurityState?.LockoutEnd is null || user.SecurityState.LockoutEnd <= timeProvider.GetUtcNow());
        return new LeaveUserAccess(user.Id, user.EmployeeId, active, permissions);
    }

    public async Task<IReadOnlyList<LeaveEmployeeInfo>> GetEmployeesAsync(long? managerId, CancellationToken cancellationToken)
    {
        var query = dbContext.Employees.AsNoTracking();
        if (managerId.HasValue) query = query.Where(e => e.ManagerId == managerId.Value);
        return await query.Select(e => new LeaveEmployeeInfo(e.Id, e.Code, e.FullName, e.Department.Name, e.ManagerId, e.DepartmentId))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkResignedAsync(long employeeId, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees.SingleAsync(e => e.Id == employeeId, cancellationToken);
        employee.Status = "RESIGNED";
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AuditDecisionAsync(Guid requestId, long employeeId, long actorUserId, string status,
        bool automatic, string? reason, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(), ActorUserId = actorUserId, EntityType = "LeaveRequest", EntityId = requestId.ToString(),
            Action = automatic ? "leave.auto-approved" : "leave." + status.ToLowerInvariant(),
            AfterJson = JsonSerializer.Serialize(new { EmployeeId = employeeId, Status = status, Automatic = automatic, Reason = reason }),
            CreatedAt = timeProvider.GetUtcNow()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
