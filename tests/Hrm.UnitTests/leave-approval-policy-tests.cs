using Hrm.Contracts;
using Hrm.Modules.Auth.Contracts;
using Hrm.Modules.Leave.Application;

namespace Hrm.UnitTests;

public sealed class LeaveApprovalPolicyTests
{
    [Theory]
    [InlineData("ANNUAL", true, true)]
    [InlineData("SICK", true, true)]
    [InlineData("MATERNITY", true, true)]
    [InlineData("RESIGNATION", true, false)]
    [InlineData("resignation", true, false)]
    [InlineData("UNKNOWN", true, false)]
    [InlineData("ANNUAL", false, false)]
    public void AutomaticApprovalExcludesResignationsAndInactiveAccounts(string type, bool active, bool expected)
    {
        var actor = new LeaveUserAccess(1, 10, active, [PermissionCodes.LeaveSelfApproved]);
        Assert.Equal(expected, LeaveApprovalPolicy.CanAutoApprove(actor, type));
    }

    [Theory]
    [InlineData("leave.approve", true)]
    [InlineData("LEAVE_APPROVE", true)]
    [InlineData("leave.team.approve", true)]
    [InlineData("leave.hr.approve", true)]
    [InlineData("self_approved", false)]
    [InlineData("employee.write", false)]
    [InlineData("ADMIN", false)]
    public void AssignedManagerNeedsApprovalPermission(string permission, bool expected)
    {
        var actor = new LeaveUserAccess(1, 10, true, [permission]);
        Assert.Equal(expected, LeaveApprovalPolicy.CanReview(actor, 20, 10));
    }

    [Theory]
    [InlineData(20L, 10L, true, true)]
    [InlineData(20L, 30L, true, false)]
    [InlineData(20L, null, true, false)]
    [InlineData(10L, 10L, true, false)]
    [InlineData(20L, 10L, false, false)]
    public void ApprovalIsLimitedToActiveDirectManager(long employeeId, long? managerId, bool active, bool expected)
    {
        var actor = new LeaveUserAccess(1, 10, active, [PermissionCodes.LeaveApprove]);
        Assert.Equal(expected, LeaveApprovalPolicy.CanReview(actor, employeeId, managerId));
    }

    [Fact]
    public void ApprovalPermissionDoesNotGrantAutomaticApproval()
    {
        var actor = new LeaveUserAccess(1, 10, true, [PermissionCodes.LeaveApprove]);
        Assert.False(LeaveApprovalPolicy.HasPermission(actor, PermissionCodes.LeaveSelfApproved));
    }
}
