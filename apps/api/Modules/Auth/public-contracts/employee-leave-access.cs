namespace Hrm.Modules.Auth.Contracts;

public sealed record LeaveUserAccess(long UserId, long EmployeeId, bool IsActive, IReadOnlyList<string> Permissions);
public sealed record LeaveEmployeeInfo(long Id, string Code, string FullName, string DepartmentName, long? ManagerId, long DepartmentId = 0);

public interface IEmployeeLeaveAccess
{
    Task<LeaveUserAccess> GetUserAsync(long userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveEmployeeInfo>> GetEmployeesAsync(long? managerId, CancellationToken cancellationToken);
    Task MarkResignedAsync(long employeeId, CancellationToken cancellationToken);
    Task AuditDecisionAsync(Guid requestId, long employeeId, long actorUserId, string status, bool automatic, string? reason, CancellationToken cancellationToken);
}
