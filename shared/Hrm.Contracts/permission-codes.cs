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

    public static IReadOnlyList<PermissionDefinition> Catalog { get; } =
    [
        new(AttendanceSelfWrite, "Chấm công vào/ra cá nhân", "Chấm công", "Cho phép nhân viên tự điểm danh vào/ra ca làm việc hàng ngày", ["ATT_CHECKIN", "attendance.write"]),
        new(AttendanceTeamApprove, "Duyệt chấm công nhóm", "Chấm công", "Theo dõi và xác nhận dữ liệu chấm công của các thành viên trong nhóm", ["ATT_APPROVE"]),
        new(LeaveSelfCreate, "Tạo đơn xin nghỉ phép", "Nghỉ phép", "Tạo và gửi yêu cầu xin nghỉ phép, nghỉ ốm, nghỉ bù", ["LEAVE_REQUEST", "leave.create"]),
        new(LeaveTeamApprove, "Duyệt đơn nghỉ phép nhóm", "Nghỉ phép", "Trưởng nhóm / Quản lý xét duyệt đơn nghỉ của nhân viên", ["LEAVE_APPROVE"]),
        new(LeaveHrApprove, "Duyệt phép cấp HR", "Nghỉ phép", "Bộ phận Nhân sự phê duyệt chính thức và cập nhật phép năm", ["LEAVE_HR_APPROVE"]),
        new(PayrollSelfRead, "Xem phiếu lương cá nhân", "Tiền lương", "Xem chi tiết bảng lương, các khoản phụ cấp và khấu trừ cá nhân", ["PAYROLL_VIEW", "payroll.read"]),
        new(PayrollRun, "Tính & Chốt bảng lương", "Tiền lương", "Tổng hợp công và tính lương cho nhân viên toàn đơn vị", ["PAYROLL_MANAGE"]),
        new(PayrollFinalize, "Duyệt chi lương chính thức", "Tiền lương", "Khóa sổ bảng lương và xuất phiếu chi trả", ["PAYROLL_FINALIZE"]),
        new(EmployeeRead, "Tra cứu Danh bạ Nhân sự", "Nhân sự", "Chỉ xem danh bạ và tra cứu thông tin nhân sự công ty (Read-only)", ["EMP_VIEW"]),
        new(EmployeeWrite, "Quản lý & Thêm mới Nhân sự", "Nhân sự", "Tiếp nhận nhân sự mới (Onboarding) và chỉnh sửa thông tin hồ sơ", ["EMP_EDIT"]),
        new(EmployeeTransfer, "Điều chuyển phòng ban", "Nhân sự", "Thực hiện điều chuyển công tác, chuyển phòng ban cho nhân sự", ["EMP_TRANSFER"]),
        new(EmployeeOffboard, "Thực hiện thôi việc", "Nhân sự", "Quyết định thôi việc và khóa quyền truy cập của nhân sự", ["EMP_OFFBOARD"]),
        new(ReportRead, "Báo cáo & Thống kê Nhân sự", "Báo cáo", "Xem các chỉ số KPI, biến động nhân sự, thâm niên và quỹ lương", ["REPORT_VIEW"]),
        new(AdminUserRead, "Xem danh sách tài khoản", "Quản trị", "Xem danh sách tài khoản người dùng hệ thống", ["admin.users.read"]),
        new(AdminUserSearch, "Tìm kiếm nâng cao tài khoản", "Quản trị", "Tìm kiếm, lọc tài khoản theo tiêu chí chi tiết", ["admin.users.search"]),
        new(AdminUserManage, "Quản lý tài khoản", "Quản trị", "Tạo tài khoản, khóa, mở khóa, đổi mật khẩu", ["USER_MANAGE"]),
        new(AdminRbacManage, "Quản trị vai trò & quyền", "Quản trị", "Tạo vai trò và phân bổ quyền hạn (RBAC)", ["RBAC_MANAGE"]),
        new(AuditRead, "Nhật ký kiểm toán", "Hệ thống", "Tra cứu lịch sử thao tác và vết hệ thống", ["AUDIT_VIEW"])
    ];

    private static readonly Dictionary<string, string> AliasMap = BuildAliasMap();

    private static Dictionary<string, string> BuildAliasMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in Catalog)
        {
            map[def.Code] = def.Code;
            foreach (var alias in def.Aliases)
            {
                map[alias] = def.Code;
            }
        }
        return map;
    }

    /// <summary>
    /// Chuyển đổi mã quyền bất kỳ (bao gồm alias viết tắt) về mã chuẩn chuẩn hóa.
    /// </summary>
    public static string Canonicalize(string permission) =>
        AliasMap.TryGetValue(permission.Trim(), out var canonical) ? canonical : permission.Trim();

    /// <summary>
    /// Mở rộng danh sách quyền để bao gồm cả mã chuẩn và alias, giúp hệ thống tương thích 100%.
    /// </summary>
    public static IReadOnlyList<string> ExpandWithAliases(IEnumerable<string> permissions)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in permissions)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            var trimmed = p.Trim();
            result.Add(trimmed);

            if (AliasMap.TryGetValue(trimmed, out var canonical))
            {
                result.Add(canonical);
                var def = Catalog.FirstOrDefault(d => string.Equals(d.Code, canonical, StringComparison.OrdinalIgnoreCase));
                if (def != null)
                {
                    foreach (var a in def.Aliases) result.Add(a);
                }
            }
        }
        return result.ToArray();
    }

    /// <summary>
    /// Lấy tên tiếng Việt thân thiện của quyền.
    /// </summary>
    public static string GetDisplayName(string permission)
    {
        var canonical = Canonicalize(permission);
        var def = Catalog.FirstOrDefault(d => string.Equals(d.Code, canonical, StringComparison.OrdinalIgnoreCase));
        return def?.Name ?? permission;
    }

    /// <summary>
    /// Lấy phân nhóm của quyền.
    /// </summary>
    public static string GetCategory(string permission)
    {
        var canonical = Canonicalize(permission);
        var def = Catalog.FirstOrDefault(d => string.Equals(d.Code, canonical, StringComparison.OrdinalIgnoreCase));
        return def?.Category ?? "Chức năng khác";
    }
}

public sealed record PermissionDefinition(
    string Code,
    string Name,
    string Category,
    string Description,
    IReadOnlyList<string> Aliases);

