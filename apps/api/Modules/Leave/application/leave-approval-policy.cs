using Hrm.Contracts;
using Hrm.Modules.Auth.Contracts;

namespace Hrm.Modules.Leave.Application;

public static class LeaveApprovalPolicy
{
    public static bool CanAutoApprove(LeaveUserAccess actor, string leaveType) =>
        (string.Equals(leaveType, LeaveTypes.Annual, StringComparison.OrdinalIgnoreCase)
            || string.Equals(leaveType, LeaveTypes.Sick, StringComparison.OrdinalIgnoreCase)
            || string.Equals(leaveType, LeaveTypes.Maternity, StringComparison.OrdinalIgnoreCase))
        && HasPermission(actor, PermissionCodes.LeaveSelfApproved);

    public static bool HasPermission(LeaveUserAccess actor, string permission) =>
        actor.IsActive && PermissionCodes.ExpandWithAliases(actor.Permissions).Contains(permission, StringComparer.OrdinalIgnoreCase);

    public static bool CanReview(LeaveUserAccess actor, long employeeId, long? managerId) =>
        actor.EmployeeId != employeeId
        && (HasPermission(actor, PermissionCodes.LeaveApproveAll)
            || (managerId == actor.EmployeeId && HasPermission(actor, PermissionCodes.LeaveApprove)));
}
