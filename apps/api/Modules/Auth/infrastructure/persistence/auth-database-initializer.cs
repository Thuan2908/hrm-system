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
            """, cancellationToken);

        foreach (var code in PermissionCodes.All)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO permissions (perm_id, permission_code, description)
                SELECT COALESCE(MAX(perm_id), 0) + 1, {code}, {"Quyền hệ thống Milestone 2"}
                FROM permissions
                HAVING NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = {code});
                """, cancellationToken);
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
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
