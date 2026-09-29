namespace Hrm.Contracts;

public static class PermissionCodes
{
    public const string EmployeeRead = "employee.read";
    public const string EmployeeWrite = "employee.write";
    public const string EmployeeTransfer = "employee.transfer";
    public const string EmployeeOffboard = "employee.offboard";
    public const string AttendanceSelfWrite = "attendance.self.write";
    public const string AttendanceTeamApprove = "attendance.team.approve";
    public const string LeaveSelfCreate = "leave.self.create";
    public const string LeaveTeamApprove = "leave.team.approve";
    public const string LeaveHrApprove = "leave.hr.approve";
    public const string PayrollRun = "payroll.run";
    public const string PayrollFinalize = "payroll.finalize";
    public const string PayrollSelfRead = "payroll.self.read";
    public const string ReportRead = "report.read";
    public const string AdminUserRead = "admin.user.read";
    public const string AdminUserSearch = "admin.user.search";
    public const string AdminUserManage = "admin.user.manage";
    public const string AdminRbacManage = "admin.rbac.manage";
    public const string AuditRead = "audit.read";

    public static IReadOnlyList<string> All { get; } =
    [
        EmployeeRead, EmployeeWrite, EmployeeTransfer, EmployeeOffboard,
        AttendanceSelfWrite, AttendanceTeamApprove,
        LeaveSelfCreate, LeaveTeamApprove, LeaveHrApprove,
        PayrollRun, PayrollFinalize, PayrollSelfRead, ReportRead,
        AdminUserRead, AdminUserSearch, AdminUserManage, AdminRbacManage, AuditRead
    ];
}
