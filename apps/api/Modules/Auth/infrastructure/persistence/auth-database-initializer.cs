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

            -- Bổ sung các cột hồ sơ nhân viên nếu chưa tồn tại
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS dob date;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS gender varchar(20);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS phone varchar(50);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS email varchar(255);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS address text;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS education_level varchar(100);
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS hire_date date;
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS status varchar(50) DEFAULT 'ACTIVE';
            ALTER TABLE employees ADD COLUMN IF NOT EXISTS created_at timestamp with time zone DEFAULT now();

            -- Chuẩn hóa hire_date là mốc duy nhất tính thâm niên
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'public' AND table_name = 'employees' AND column_name = 'join_date'
                ) THEN
                    UPDATE employees SET hire_date = COALESCE(hire_date, join_date, CURRENT_DATE) WHERE hire_date IS NULL;
                ELSE
                    UPDATE employees SET hire_date = COALESCE(hire_date, CURRENT_DATE) WHERE hire_date IS NULL;
                END IF;
            END $$;

            -- Chuẩn hóa education_level về chuẩn enum mã hóa
            UPDATE employees
            SET education_level = CASE
                WHEN upper(education_level) LIKE '%THAC%' OR upper(education_level) LIKE '%THẠC%' THEN 'THAC_SI'
                WHEN upper(education_level) LIKE '%CAO DANG%' OR upper(education_level) LIKE '%CAO ĐẲNG%' OR upper(education_level) = 'CĐ' THEN 'CAO_DANG'
                WHEN upper(education_level) LIKE '%TRUNG CAP%' OR upper(education_level) LIKE '%TRUNG CẤP%' OR upper(education_level) = 'TC' THEN 'TRUNG_CAP'
                WHEN upper(education_level) LIKE '%DAI HOC%' OR upper(education_level) LIKE '%ĐẠI HỌC%' OR upper(education_level) = 'ĐH' THEN 'DAI_HOC'
                WHEN education_level IS NULL OR trim(education_level) = '' THEN 'DAI_HOC'
                ELSE 'KHAC'
            END;

            -- Bảng EMPLOYEE_SALARY_PROFILES (Chuẩn 3NF: Tách lịch sử lương khỏi bảng employees)
            CREATE TABLE IF NOT EXISTS employee_salary_profiles (
                profile_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                emp_id bigint NOT NULL,
                base_salary numeric(15,2) NOT NULL DEFAULT 0 CHECK (base_salary >= 0),
                position_allowance numeric(15,2) NOT NULL DEFAULT 0 CHECK (position_allowance >= 0),
                other_allowance numeric(15,2) NOT NULL DEFAULT 0 CHECK (other_allowance >= 0),
                insurance_salary numeric(15,2) NULL CHECK (insurance_salary >= 0),
                effective_date date NOT NULL,
                expiry_date date NULL,
                is_current boolean NOT NULL DEFAULT true,
                note text NULL,
                created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT fk_salary_profiles_employees FOREIGN KEY (emp_id) REFERENCES employees(emp_id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS uq_salary_profiles_current_emp
                ON employee_salary_profiles(emp_id)
                WHERE is_current = true;

            -- Di chuyển dữ liệu lương ban đầu sang bảng employee_salary_profiles nếu có cột base_salary
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'public' AND table_name = 'employees' AND column_name = 'base_salary'
                ) THEN
                    INSERT INTO employee_salary_profiles (emp_id, base_salary, position_allowance, other_allowance, effective_date, is_current)
                    SELECT e.emp_id, COALESCE(e.base_salary, 10000000), 0, 0, COALESCE(e.hire_date, CURRENT_DATE), true
                    FROM employees e
                    WHERE NOT EXISTS (
                        SELECT 1 FROM employee_salary_profiles sp WHERE sp.emp_id = e.emp_id
                    );
                ELSE
                    INSERT INTO employee_salary_profiles (emp_id, base_salary, position_allowance, other_allowance, effective_date, is_current)
                    SELECT e.emp_id, 10000000, 0, 0, COALESCE(e.hire_date, CURRENT_DATE), true
                    FROM employees e
                    WHERE NOT EXISTS (
                        SELECT 1 FROM employee_salary_profiles sp WHERE sp.emp_id = e.emp_id
                    );
                END IF;
            END $$;

            -- Dọn dẹp triệt để các thuộc tính dư thừa chuẩn 3NF
            ALTER TABLE employees DROP COLUMN IF EXISTS join_date;
            ALTER TABLE employees DROP COLUMN IF EXISTS base_salary;
            ALTER TABLE positions DROP COLUMN IF EXISTS title;
            ALTER TABLE positions DROP COLUMN IF EXISTS allowance_rate;

            -- Bảng PAYROLLS: Bổ sung các cột Snapshot bảo đảm tính toàn vẹn và bất biến (ADR-010)
            ALTER TABLE payrolls ADD COLUMN IF NOT EXISTS standard_working_days numeric(4,1) NOT NULL DEFAULT 22.0;
            ALTER TABLE payrolls ADD COLUMN IF NOT EXISTS snapshot_base_salary numeric(15,2) NOT NULL DEFAULT 0;
            ALTER TABLE payrolls ADD COLUMN IF NOT EXISTS snapshot_position_allowance numeric(15,2) NOT NULL DEFAULT 0;
            ALTER TABLE payrolls ADD COLUMN IF NOT EXISTS snapshot_other_allowance numeric(15,2) NOT NULL DEFAULT 0;

            ALTER TABLE departments ADD COLUMN IF NOT EXISTS dept_code varchar(50);
            ALTER TABLE positions ADD COLUMN IF NOT EXISTS position_code varchar(50);

            -- Bổ sung danh mục chức danh chuẩn cho các phòng ban nếu chưa có (tinh gọn, bỏ title & allowance_rate)
            INSERT INTO positions (position_id, position_code, position_name)
            VALUES
                (11, 'POS_11', 'Chuyên Viên Kinh Doanh'),
                (12, 'POS_12', 'Chuyên Viên Marketing & Tiếp Thị'),
                (13, 'POS_13', 'Nhân Viên Bán Hàng'),
                (14, 'POS_14', 'Chuyên Viên C&B (Tiền Lương & Đãi Ngộ)'),
                (15, 'POS_15', 'Thủ Quỹ & Ngân Quỹ'),
                (16, 'POS_16', 'Chuyên Viên Chăm Sóc Khách Hàng (CSKH)')
            ON CONFLICT (position_id) DO NOTHING;
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
