-- Manual migration: consolidated former Auth, Employees, Files, Attendance, Leave and Payroll initializers.
-- Apply manually in Supabase SQL Editor. Nothing in the application executes this file.
-- Existing legacy base tables are required: users, roles, permissions, role_permissions,
-- employees, departments, positions, payrolls. This is not an empty-database bootstrap.
-- Default execution retains historical tables/columns and excludes demo payroll rows.
-- Set the optional switches below to true only when explicitly selecting those operations.
BEGIN;
SET LOCAL search_path = public, pg_catalog;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';
SELECT set_config('hrm.manual_migration.legacy_cleanup', 'false', true);
SELECT set_config('hrm.manual_migration.demo_payroll', 'false', true);
SELECT pg_advisory_xact_lock(7246012026);
SELECT pg_advisory_xact_lock(7246012031);
DO $preflight$
DECLARE required_table text;
BEGIN
    FOREACH required_table IN ARRAY ARRAY['users', 'roles', 'permissions', 'role_permissions', 'employees', 'departments', 'positions', 'payrolls']
    LOOP
        IF to_regclass('public.' || required_table) IS NULL THEN
            RAISE EXCEPTION 'Required legacy table public.% is missing; provision it manually first.', required_table;
        END IF;
    END LOOP;
END $preflight$;

-- Preflight: stop rather than deleting incompatible leave history.
DO $leave_schema$
BEGIN
    IF to_regclass('public.leave_requests') IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns WHERE table_schema = 'public'
            AND table_name = 'leave_requests' AND column_name = 'start_date'
    ) THEN
        RAISE EXCEPTION 'Existing leave_requests has a legacy layout. Migrate its data manually before applying this migration.';
    END IF;
END $leave_schema$;

-- 1. Auth support tables, employee columns, salary-profile backfill, payroll snapshots and position catalog.
-- The retained salary-profile backfill uses 10,000,000 when no legacy salary exists, as the former initializer did.

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
    WHEN education_level IN ('THAC_SI', 'DAI_HOC', 'CAO_DANG', 'TRUNG_CAP', 'KHAC') THEN education_level
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
-- Legacy column removal is preserved in the optional cleanup section below.

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

-- 2. Employee timeline and direct manager relationship (ADR-015).
ALTER TABLE employees ADD COLUMN IF NOT EXISTS manager_id bigint NULL;
CREATE INDEX IF NOT EXISTS ix_employees_manager_id ON employees(manager_id);
DO $manager$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = 'employees'::regclass AND conname = 'fk_employees_manager') THEN
        ALTER TABLE employees ADD CONSTRAINT fk_employees_manager
            FOREIGN KEY (manager_id) REFERENCES employees(emp_id) ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = 'employees'::regclass AND conname = 'ck_employees_manager_not_self') THEN
        ALTER TABLE employees ADD CONSTRAINT ck_employees_manager_not_self
            CHECK (manager_id IS NULL OR manager_id <> emp_id);
    END IF;
END $manager$;
CREATE TABLE IF NOT EXISTS employee_timeline_events (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    emp_id bigint NOT NULL,
    event_type varchar(100) NOT NULL,
    title varchar(255) NOT NULL,
    description text NOT NULL,
    timestamp timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id bigint NULL,
    created_by_name varchar(255) NULL
);

CREATE INDEX IF NOT EXISTS ix_employee_timeline_events_emp_id
    ON employee_timeline_events(emp_id);

DO $$
BEGIN
    -- FK: employee_timeline_events -> employees
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'employee_timeline_events'::regclass AND conname = 'fk_employeetimelineevents_employees'
    ) THEN
        ALTER TABLE employee_timeline_events
        ADD CONSTRAINT fk_employeetimelineevents_employees
        FOREIGN KEY (emp_id) REFERENCES employees(emp_id) ON DELETE CASCADE;
    END IF;

    -- FK: employee_timeline_events -> users
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'employee_timeline_events'::regclass AND conname = 'fk_employeetimelineevents_users'
    ) THEN
        ALTER TABLE employee_timeline_events
        ADD CONSTRAINT fk_employeetimelineevents_users
        FOREIGN KEY (created_by_user_id) REFERENCES users(user_id) ON DELETE SET NULL;
    END IF;
END $$;

-- 3. File attachment metadata (created before leave attachment foreign keys).

CREATE TABLE IF NOT EXISTS file_attachments (
    id uuid PRIMARY KEY,
    file_name varchar(255) NOT NULL,
    content_type varchar(128) NOT NULL,
    file_size_bytes bigint NOT NULL,
    storage_path varchar(512) NOT NULL,
    module varchar(64) NOT NULL,
    reference_id varchar(128) NULL,
    uploaded_by_user_id bigint NOT NULL,
    created_at timestamp with time zone NOT NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    deleted_at timestamp with time zone NULL
);

CREATE INDEX IF NOT EXISTS ix_file_attachments_module_ref
    ON file_attachments(module, reference_id);

CREATE INDEX IF NOT EXISTS ix_file_attachments_uploaded_by
    ON file_attachments(uploaded_by_user_id);

CREATE INDEX IF NOT EXISTS ix_file_attachments_created_at
    ON file_attachments(created_at);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'file_attachments'::regclass AND conname = 'fk_fileattachments_users'
    ) THEN
        ALTER TABLE file_attachments
        ADD CONSTRAINT fk_fileattachments_users
        FOREIGN KEY (uploaded_by_user_id) REFERENCES users(user_id) ON DELETE RESTRICT;
    END IF;
END $$;

-- 4. Attendance and leave schema.
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

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'time_attendances'::regclass AND conname = 'fk_timeattendances_employees'
    ) THEN
        ALTER TABLE time_attendances
        ADD CONSTRAINT fk_timeattendances_employees
        FOREIGN KEY (emp_id) REFERENCES employees(emp_id) ON DELETE RESTRICT;
    END IF;
END $$;

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

ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS attachment_file_id uuid NULL;
ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS attachment_file_name text NULL;
ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS approved_by_user_id bigint NULL;
ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS approved_at timestamp with time zone NULL;

DO $$
BEGIN
    -- 1. FK tới employees
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'leave_requests'::regclass AND conname = 'fk_leaverequests_employees'
    ) THEN
        ALTER TABLE leave_requests
        ADD CONSTRAINT fk_leaverequests_employees
        FOREIGN KEY (emp_id) REFERENCES employees(emp_id) ON DELETE RESTRICT;
    END IF;

    -- 2. FK tới users (người duyệt)
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'leave_requests'::regclass AND conname = 'fk_leaverequests_approved_by_user'
    ) THEN
        ALTER TABLE leave_requests
        ADD CONSTRAINT fk_leaverequests_approved_by_user
        FOREIGN KEY (approved_by_user_id) REFERENCES users(user_id) ON DELETE SET NULL;
    END IF;

    -- 3. FK tới file_attachments (file đính kèm)
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conrelid = 'leave_requests'::regclass AND conname = 'fk_leaverequests_file_attachments'
    ) AND EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'file_attachments'
    ) THEN
        ALTER TABLE leave_requests
        ADD CONSTRAINT fk_leaverequests_file_attachments
        FOREIGN KEY (attachment_file_id) REFERENCES file_attachments(id) ON DELETE SET NULL;
    END IF;
END $$;

ALTER TABLE leave_requests ADD COLUMN IF NOT EXISTS is_auto_approved boolean NOT NULL DEFAULT false;

-- 5. Payroll index.

CREATE INDEX IF NOT EXISTS ix_payrolls_emp_id
    ON payrolls(emp_id);

-- 6. Permission catalog: resolve former C# interpolated seeds to standalone PostgreSQL.
-- self_approved is created but never granted by this migration.
LOCK TABLE permissions IN EXCLUSIVE MODE;
DO $permissions$
DECLARE permission_record record;
BEGIN
    FOR permission_record IN SELECT * FROM (VALUES
        ('attendance.self.write', 'Chấm công vào/ra cá nhân'),
        ('attendance.team.read', 'Xem & Theo dõi chấm công bộ phận'),
        ('leave.self.create', 'Tạo đơn xin nghỉ phép'),
        ('self_approved', 'Tự động duyệt đơn cá nhân'),
        ('leave.approve', 'Duyệt đơn nghỉ phép'),
        ('payroll.self.read', 'Xem phiếu lương cá nhân'),
        ('payroll.run', 'Tính & Chốt bảng lương'),
        ('payroll.finalize', 'Duyệt chi lương chính thức'),
        ('employee.read', 'Tra cứu Danh bạ Nhân sự'),
        ('employee.write', 'Quản lý & Thêm mới Nhân sự'),
        ('employee.transfer', 'Điều chuyển phòng ban'),
        ('employee.promote', 'Bổ nhiệm & Thăng chức'),
        ('employee.offboard', 'Thực hiện thôi việc'),
        ('report.read', 'Báo cáo & Thống kê Nhân sự'),
        ('admin.user.read', 'Xem danh sách tài khoản'),
        ('admin.user.search', 'Tìm kiếm nâng cao tài khoản'),
        ('admin.user.manage', 'Quản lý tài khoản'),
        ('admin.rbac.manage', 'Quản trị vai trò & quyền'),
        ('audit.read', 'Nhật ký kiểm toán')
    ) AS catalog(code, description)
    LOOP
        INSERT INTO permissions (perm_id, permission_code, description)
        SELECT COALESCE(MAX(perm_id), 0) + 1, permission_record.code, permission_record.description
        FROM permissions
        HAVING NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = permission_record.code);
        UPDATE permissions SET description = permission_record.description
        WHERE permission_code = permission_record.code
          AND (description IS NULL OR description LIKE 'Quyền hệ thống%');
    END LOOP;
END $permissions$;

-- 7. Legacy permission aliases and former baseline role grants (excluding self_approved).

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
  AND p.permission_code <> 'self_approved'
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);

INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE upper(r.role_name) IN ('EMPLOYEE', 'ADMIN', 'HR_MANAGER')
  AND p.permission_code = 'attendance.self.write'
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);

INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE upper(r.role_name) IN ('EMPLOYEE', 'ADMIN', 'HR_MANAGER')
  AND p.permission_code = 'leave.self.create'
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);

INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE upper(r.role_name) IN ('EMPLOYEE', 'ADMIN', 'HR_MANAGER')
  AND p.permission_code = 'payroll.self.read'
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.perm_id = p.perm_id);

-- 8. Optional legacy cleanup. Enabling this removes the former obsolete columns and payslips table.
-- The former leave initializer discarded incompatible leave history. Preserve it here and require
-- a separate manual data migration instead of dropping leave_requests, even with cleanup enabled.
DO $cleanup$
BEGIN
    IF current_setting('hrm.manual_migration.legacy_cleanup')::boolean THEN
ALTER TABLE employees DROP COLUMN IF EXISTS join_date;
ALTER TABLE employees DROP COLUMN IF EXISTS base_salary;
ALTER TABLE positions DROP COLUMN IF EXISTS title;
ALTER TABLE positions DROP COLUMN IF EXISTS allowance_rate;
DROP TABLE IF EXISTS payslips CASCADE;
    END IF;
END $cleanup$;

-- Synchronize the legacy payroll identity sequence before optional demo insertion.

DO $$
DECLARE
    max_id bigint;
    seq_name text;
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'payrolls') THEN
        SELECT COALESCE(MAX(payroll_id), 0) INTO max_id FROM payrolls;
        seq_name := pg_get_serial_sequence('payrolls', 'payroll_id');
        IF seq_name IS NOT NULL THEN
            IF max_id > 0 THEN
                PERFORM setval(seq_name, max_id, true);
            ELSE
                PERFORM setval(seq_name, 1, false);
            END IF;
        END IF;
    END IF;
END $$;

-- 9. Optional demo payroll for July/August 2026. Never inserted by default.
DO $demo$
BEGIN
    IF current_setting('hrm.manual_migration.demo_payroll')::boolean THEN
INSERT INTO payrolls (
    emp_id, month_period, year_period,
    actual_days, gross_sal, bhxh_deduct, tax_deduct, net_sal
)
SELECT
    u.emp_id,
    8,
    2026,
    25.5,
    17350000.00,
    1575000.00,
    350000.00,
    15425000.00
FROM users u
WHERE u.emp_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM payrolls p
      WHERE p.emp_id = u.emp_id AND p.month_period = 8 AND p.year_period = 2026
  );
INSERT INTO payrolls (
    emp_id, month_period, year_period,
    actual_days, gross_sal, bhxh_deduct, tax_deduct, net_sal
)
SELECT
    u.emp_id,
    7,
    2026,
    26.0,
    16500000.00,
    1575000.00,
    320000.00,
    14605000.00
FROM users u
WHERE u.emp_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM payrolls p
      WHERE p.emp_id = u.emp_id AND p.month_period = 7 AND p.year_period = 2026
  );
    END IF;
END $demo$;

DO $$
DECLARE
    max_id bigint;
    seq_name text;
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'payrolls') THEN
        SELECT COALESCE(MAX(payroll_id), 0) INTO max_id FROM payrolls;
        seq_name := pg_get_serial_sequence('payrolls', 'payroll_id');
        IF seq_name IS NOT NULL THEN
            IF max_id > 0 THEN
                PERFORM setval(seq_name, max_id, true);
            ELSE
                PERFORM setval(seq_name, 1, false);
            END IF;
        END IF;
    END IF;
END $$;

COMMIT;
