using Hrm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Leave.Infrastructure.Persistence;

public interface ILeaveDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class LeaveDatabaseInitializer(LeaveDbContext dbContext) : ILeaveDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7246012028)", cancellationToken);

        // Xử lý migration an toàn: Nếu bảng leave_requests cũ đã tồn tại nhưng không có cột start_date
        // thì xóa bảng cũ không tương thích để tái tạo bảng chuẩn.
        await dbContext.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.tables 
                    WHERE table_schema = 'public' AND table_name = 'leave_requests'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'public' AND table_name = 'leave_requests' AND column_name = 'start_date'
                ) THEN
                    DROP TABLE IF EXISTS leave_requests CASCADE;
                END IF;
            END $$;
            """, cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS leave_requests (
                id uuid PRIMARY KEY,
                emp_id bigint NOT NULL,
                leave_type varchar(50) NOT NULL,
                start_date date NOT NULL,
                end_date date NOT NULL,
                days_count numeric(4, 1) NOT NULL,
                reason text NOT NULL,
                status varchar(50) NOT NULL DEFAULT 'PENDING',
                rejection_reason text NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NULL
            );

            CREATE INDEX IF NOT EXISTS ix_leave_requests_emp_id
                ON leave_requests(emp_id);

            CREATE INDEX IF NOT EXISTS ix_leave_requests_emp_dates
                ON leave_requests(emp_id, start_date, end_date);
            """, cancellationToken);

        // Đảm bảo quyền leave.self.create tồn tại trong bảng permissions
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO permissions (perm_id, permission_code, description)
            SELECT COALESCE(MAX(perm_id), 0) + 1, {PermissionCodes.LeaveSelfCreate}, 'Nhân viên tạo đơn xin nghỉ phép'
            FROM permissions
            HAVING NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = {PermissionCodes.LeaveSelfCreate});
            """, cancellationToken);

        // Tự động gán quyền leave.self.create cho các vai trò EMPLOYEE, ADMIN, HR_MANAGER
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT r.role_id, p.perm_id
            FROM roles r
            CROSS JOIN permissions p
            WHERE upper(r.role_name) IN ('EMPLOYEE', 'ADMIN', 'HR_MANAGER')
              AND p.permission_code = {PermissionCodes.LeaveSelfCreate}
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp
                  WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
