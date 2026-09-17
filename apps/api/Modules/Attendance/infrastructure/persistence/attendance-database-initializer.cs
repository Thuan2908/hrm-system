using Hrm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Attendance.Infrastructure.Persistence;

public interface IAttendanceDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class AttendanceDatabaseInitializer(AttendanceDbContext dbContext) : IAttendanceDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7246012027)", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS time_attendances (
                id uuid PRIMARY KEY,
                emp_id bigint NOT NULL,
                work_date date NOT NULL,
                check_in_time timestamp with time zone NOT NULL,
                check_out_time timestamp with time zone NULL,
                actual_hours numeric(5, 2) NULL,
                status varchar(50) NOT NULL DEFAULT 'PRESENT',
                notes text NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_time_attendances_emp_date
                ON time_attendances(emp_id, work_date);

            CREATE INDEX IF NOT EXISTS ix_time_attendances_work_date
                ON time_attendances(work_date);
            """, cancellationToken);

        // Đảm bảo quyền attendance.self.write tồn tại trong bảng permissions
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO permissions (perm_id, permission_code, description)
            SELECT COALESCE(MAX(perm_id), 0) + 1, {PermissionCodes.AttendanceSelfWrite}, 'Nhân viên tự chấm công (Check-in/Check-out)'
            FROM permissions
            HAVING NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = {PermissionCodes.AttendanceSelfWrite});
            """, cancellationToken);

        // Tự động gán quyền attendance.self.write cho vai trò EMPLOYEE
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT r.role_id, p.perm_id
            FROM roles r
            CROSS JOIN permissions p
            WHERE upper(r.role_name) IN ('EMPLOYEE', 'ADMIN', 'HR_MANAGER')
              AND p.permission_code = {PermissionCodes.AttendanceSelfWrite}
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp
                  WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
