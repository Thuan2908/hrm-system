using Hrm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Auth.Infrastructure.Persistence;

public interface IAuthDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class AuthDatabaseInitializer(AuthDbContext dbContext) : IAuthDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(7246012026)", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS user_security_states (
                user_id bigint PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
                failed_access_count integer NOT NULL DEFAULT 0,
                lockout_end timestamp with time zone NULL
            );

            CREATE TABLE IF NOT EXISTS user_account_metadata (
                user_id bigint PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
                created_at timestamp with time zone NOT NULL
            );
            INSERT INTO user_account_metadata (user_id, created_at)
            SELECT user_id, COALESCE(last_login AT TIME ZONE 'UTC', now())
            FROM users
            ON CONFLICT (user_id) DO NOTHING;

            CREATE TABLE IF NOT EXISTS refresh_tokens (
                id uuid PRIMARY KEY,
                user_id bigint NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
                token_hash varchar(128) NOT NULL UNIQUE,
                expires_at timestamp with time zone NOT NULL,
                created_at timestamp with time zone NOT NULL,
                created_by_ip varchar(64) NULL,
                revoked_at timestamp with time zone NULL,
                revoked_by_ip varchar(64) NULL,
                replaced_by_token_hash varchar(128) NULL
            );
            CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id_expires_at
                ON refresh_tokens(user_id, expires_at);

            CREATE TABLE IF NOT EXISTS audit_logs (
                id uuid PRIMARY KEY,
                actor_user_id bigint NULL REFERENCES users(user_id) ON DELETE SET NULL,
                action varchar(120) NOT NULL,
                entity_type varchar(120) NOT NULL,
                entity_id varchar(120) NOT NULL,
                before_json jsonb NULL,
                after_json jsonb NULL,
                created_at timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_audit_logs_created_at ON audit_logs(created_at);
            CREATE INDEX IF NOT EXISTS ix_audit_logs_entity ON audit_logs(entity_type, entity_id);

            CREATE TABLE IF NOT EXISTS user_active_sessions (
                user_id bigint PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
                device_id text NOT NULL,
                ip_address varchar(64) NULL,
                user_agent text NULL,
                created_at timestamp with time zone NOT NULL,
                last_seen_at timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_user_active_sessions_last_seen_at ON user_active_sessions(last_seen_at);
            """, cancellationToken);

        foreach (var def in PermissionCodes.Catalog)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO permissions (perm_id, permission_code, description)
                SELECT COALESCE(MAX(perm_id), 0) + 1, {def.Code}, {def.Name}
                FROM permissions
                HAVING NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = {def.Code});
                """, cancellationToken);

            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE permissions
                SET description = {def.Name}
                WHERE permission_code = {def.Code}
                  AND (description IS NULL OR description LIKE 'Quyền hệ thống%');
                """, cancellationToken);
        }

        // Tự động nâng cấp các mã quyền cũ/viết tắt (ATT_CHECKIN, EMP_VIEW, EMP_EDIT, PAYROLL_MANAGE) sang mã chuẩn
        await dbContext.Database.ExecuteSqlRawAsync("""
            -- ATT_CHECKIN -> attendance.self.write
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT rp.role_id, p_new.perm_id
            FROM role_permissions rp
            JOIN permissions p_old ON rp.perm_id = p_old.perm_id AND p_old.permission_code = 'ATT_CHECKIN'
            CROSS JOIN permissions p_new
            WHERE p_new.permission_code = 'attendance.self.write'
              AND NOT EXISTS (SELECT 1 FROM role_permissions rp2 WHERE rp2.role_id = rp.role_id AND rp2.perm_id = p_new.perm_id);

            -- EMP_VIEW -> employee.read
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT rp.role_id, p_new.perm_id
            FROM role_permissions rp
            JOIN permissions p_old ON rp.perm_id = p_old.perm_id AND p_old.permission_code = 'EMP_VIEW'
            CROSS JOIN permissions p_new
            WHERE p_new.permission_code = 'employee.read'
              AND NOT EXISTS (SELECT 1 FROM role_permissions rp2 WHERE rp2.role_id = rp.role_id AND rp2.perm_id = p_new.perm_id);

            -- EMP_EDIT -> employee.write
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT rp.role_id, p_new.perm_id
            FROM role_permissions rp
            JOIN permissions p_old ON rp.perm_id = p_old.perm_id AND p_old.permission_code = 'EMP_EDIT'
            CROSS JOIN permissions p_new
            WHERE p_new.permission_code = 'employee.write'
              AND NOT EXISTS (SELECT 1 FROM role_permissions rp2 WHERE rp2.role_id = rp.role_id AND rp2.perm_id = p_new.perm_id);

            -- PAYROLL_MANAGE -> payroll.run, payroll.finalize
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT rp.role_id, p_new.perm_id
            FROM role_permissions rp
            JOIN permissions p_old ON rp.perm_id = p_old.perm_id AND p_old.permission_code = 'PAYROLL_MANAGE'
            CROSS JOIN permissions p_new
            WHERE p_new.permission_code IN ('payroll.run', 'payroll.finalize')
              AND NOT EXISTS (SELECT 1 FROM role_permissions rp2 WHERE rp2.role_id = rp.role_id AND rp2.perm_id = p_new.perm_id);

            -- Đảm bảo HR_STAFF có đầy đủ quyền tự phục vụ (Self-service)
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT r.role_id, p.perm_id
            FROM roles r
            CROSS JOIN permissions p
            WHERE upper(r.role_name) IN ('HR_STAFF', 'HR', 'EMPLOYEE')
              AND p.permission_code IN ('attendance.self.write', 'leave.self.create', 'payroll.self.read')
              AND NOT EXISTS (SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);

            -- Cấp toàn quyền cho ADMIN
            INSERT INTO role_permissions (role_id, perm_id)
            SELECT r.role_id, p.perm_id
            FROM roles r
            CROSS JOIN permissions p
            WHERE upper(r.role_name) = 'ADMIN'
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp
                  WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
