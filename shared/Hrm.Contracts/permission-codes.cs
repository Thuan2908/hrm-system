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
    public const string ProductRead = "product.read";
    public const string ProductCreate = "product.create";
    public const string ProductUpdate = "product.update";
    public const string ProductDeactivate = "product.deactivate";
    public const string SupplierRead = "supplier.read";
    public const string SupplierCreate = "supplier.create";
    public const string SupplierUpdate = "supplier.update";
    public const string SupplierDeactivate = "supplier.deactivate";
    public const string WarehouseRead = "warehouse.read";
    public const string WarehouseManage = "warehouse.manage";
    public const string InventoryRead = "inventory.read";
    public const string InventoryAdjust = "inventory.adjust";
    public const string InventoryTransfer = "inventory.transfer";
    public const string InventoryPost = "inventory.post";
    public const string ProcurementRead = "procurement.read";
    public const string ProcurementCreate = "procurement.create";
    public const string ProcurementApprove = "procurement.approve";
    public const string ProcurementReceive = "procurement.receive";
    public const string SalesRead = "sales.read";
    public const string SalesCreate = "sales.create";
    public const string SalesUpdate = "sales.update";
    public const string SalesConfirm = "sales.confirm";
    public const string SalesReportRead = "sales.report.read";

    public static IReadOnlyList<string> All { get; } =
    [
        EmployeeRead, EmployeeWrite, EmployeeTransfer, EmployeeOffboard,
        AttendanceSelfWrite, AttendanceTeamApprove,
        LeaveSelfCreate, LeaveTeamApprove, LeaveHrApprove,
        PayrollRun, PayrollFinalize, PayrollSelfRead, ReportRead,
        AdminUserRead, AdminUserSearch, AdminUserManage, AdminRbacManage, AuditRead,
        ProductRead, ProductCreate, ProductUpdate, ProductDeactivate,
        SupplierRead, SupplierCreate, SupplierUpdate, SupplierDeactivate,
        WarehouseRead, WarehouseManage,
        InventoryRead, InventoryAdjust, InventoryTransfer, InventoryPost,
        ProcurementRead, ProcurementCreate, ProcurementApprove, ProcurementReceive,
        SalesRead, SalesCreate, SalesUpdate, SalesConfirm, SalesReportRead
    ];
}
