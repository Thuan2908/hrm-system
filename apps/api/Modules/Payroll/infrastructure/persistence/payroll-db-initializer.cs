using Hrm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Payroll.Infrastructure.Persistence;

public interface IPayrollDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class PayrollDatabaseInitializer(PayrollDbContext dbContext) : IPayrollDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7246012029)", cancellationToken);

        // 1. Xóa bảng payslips theo yêu cầu để tránh dư thừa dữ liệu
        await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS payslips CASCADE;", cancellationToken);

        // 2. Đảm bảo index trên bảng payrolls có sẵn
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_payrolls_emp_id
                ON payrolls(emp_id);
            """, cancellationToken);

        // 3. Đảm bảo quyền payroll.self.read tồn tại trong bảng permissions
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO permissions (perm_id, permission_code, description)
            SELECT COALESCE(MAX(perm_id), 0) + 1, {PermissionCodes.PayrollSelfRead}, 'Nhân viên xem phiếu lương cá nhân'
            FROM permissions
            HAVING NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = {PermissionCodes.PayrollSelfRead});
            """, cancellationToken);

        // 4. Tự động gán quyền payroll.self.read cho các vai trò EMPLOYEE, ADMIN, HR_MANAGER
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT r.role_id, p.perm_id
            FROM roles r
            CROSS JOIN permissions p
            WHERE upper(r.role_name) IN ('EMPLOYEE', 'ADMIN', 'HR_MANAGER')
              AND p.permission_code = {PermissionCodes.PayrollSelfRead}
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp
                  WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
