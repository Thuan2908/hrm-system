-- ==============================================================================
-- HRM SYSTEM - SUPABASE SCHEMA & MANUAL SEED DATA SCRIPT
-- ==============================================================================
-- Script này được dùng để chạy thủ công trên Supabase SQL Editor khi cần dựng mới
-- hoặc khôi phục dữ liệu ban đầu. Hệ thống KHÔNG tự động chạy script này khi khởi động.
-- ==============================================================================

-- 1. EXTENSIONS
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- 2. BẢNG PHÒNG BAN & CHỨC DANH
CREATE TABLE IF NOT EXISTS departments (
    dept_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    dept_code varchar(50) UNIQUE NOT NULL,
    dept_name varchar(255) NOT NULL,
    description text NULL
);

CREATE TABLE IF NOT EXISTS positions (
    position_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    position_code varchar(50) UNIQUE NOT NULL,
    position_name varchar(255) NOT NULL
);

-- 3. BẢNG HỒ SƠ NHÂN SỰ (3NF)
CREATE TABLE IF NOT EXISTS employees (
    emp_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    emp_code varchar(50) UNIQUE NOT NULL,
    full_name varchar(255) NOT NULL,
    dept_id bigint NULL REFERENCES departments(dept_id) ON DELETE SET NULL,
    position_id bigint NULL REFERENCES positions(position_id) ON DELETE SET NULL,
    dob date NULL,
    gender varchar(20) NULL,
    phone varchar(50) NULL,
    email varchar(255) NULL,
    address text NULL,
    education_level varchar(100) DEFAULT 'DAI_HOC',
    hire_date date NOT NULL DEFAULT CURRENT_DATE,
    status varchar(50) NOT NULL DEFAULT 'ACTIVE',
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_employees_dept_id ON employees(dept_id);
CREATE INDEX IF NOT EXISTS ix_employees_status ON employees(status);

-- 4. BẢNG HỒ SƠ LƯƠNG NHÂN VIÊN (Chuẩn 3NF tách rời lịch sử lương)
CREATE TABLE IF NOT EXISTS employee_salary_profiles (
    profile_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    emp_id bigint NOT NULL REFERENCES employees(emp_id) ON DELETE CASCADE,
    base_salary numeric(15,2) NOT NULL DEFAULT 0 CHECK (base_salary >= 0),
    position_allowance numeric(15,2) NOT NULL DEFAULT 0 CHECK (position_allowance >= 0),
    other_allowance numeric(15,2) NOT NULL DEFAULT 0 CHECK (other_allowance >= 0),
    insurance_salary numeric(15,2) NULL CHECK (insurance_salary >= 0),
    effective_date date NOT NULL DEFAULT CURRENT_DATE,
    expiry_date date NULL,
    is_current boolean NOT NULL DEFAULT true,
    note text NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_salary_profiles_current_emp
    ON employee_salary_profiles(emp_id)
    WHERE is_current = true;

-- 5. BẢNG VAI TRÒ & PHÂN QUYỀN (RBAC)
CREATE TABLE IF NOT EXISTS roles (
    role_id bigint PRIMARY KEY,
    role_name varchar(100) UNIQUE NOT NULL,
    description text NULL
);

CREATE TABLE IF NOT EXISTS permissions (
    perm_id bigint PRIMARY KEY,
    permission_code varchar(100) UNIQUE NOT NULL,
    description text NULL
);

CREATE TABLE IF NOT EXISTS role_permissions (
    role_id bigint NOT NULL REFERENCES roles(role_id) ON DELETE CASCADE,
    perm_id bigint NOT NULL REFERENCES permissions(perm_id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, perm_id)
);

-- 6. BẢNG TÀI KHOẢN NGƯỜI DÙNG (USERS)
CREATE TABLE IF NOT EXISTS users (
    user_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    emp_id bigint NULL REFERENCES employees(emp_id) ON DELETE SET NULL,
    role_id bigint NOT NULL REFERENCES roles(role_id) ON DELETE RESTRICT,
    username varchar(100) UNIQUE NOT NULL,
    password_hash text NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    last_login timestamp with time zone NULL,
    deleted_at timestamp with time zone NULL
);

CREATE INDEX IF NOT EXISTS ix_users_emp_id ON users(emp_id);
CREATE INDEX IF NOT EXISTS ix_users_deleted_at ON users(deleted_at);

-- 7. BẢNG BẢO MẬT TÀI KHOẢN (LOCKOUT & REFRESH TOKENS & AUDIT)
CREATE TABLE IF NOT EXISTS user_security_states (
    user_id bigint PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
    failed_access_count integer NOT NULL DEFAULT 0,
    lockout_end timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS user_account_metadata (
    user_id bigint PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

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

CREATE TABLE IF NOT EXISTS user_active_sessions (
    user_id bigint PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
    device_id text NOT NULL,
    ip_address varchar(64) NULL,
    user_agent text NULL,
    created_at timestamp with time zone NOT NULL,
    last_seen_at timestamp with time zone NOT NULL
);

CREATE TABLE IF NOT EXISTS audit_logs (
    id uuid PRIMARY KEY,
    actor_user_id bigint NULL REFERENCES users(user_id) ON DELETE SET NULL,
    action varchar(120) NOT NULL,
    entity_type varchar(120) NOT NULL,
    entity_id varchar(120) NOT NULL,
    before_json jsonb NULL,
    after_json jsonb NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS ix_audit_logs_created_at ON audit_logs(created_at);

-- 8. BẢNG CHẤM CÔNG (TIME ATTENDANCE)
CREATE TABLE IF NOT EXISTS time_attendances (
    id uuid PRIMARY KEY,
    emp_id bigint NOT NULL REFERENCES employees(emp_id) ON DELETE RESTRICT,
    work_date date NOT NULL,
    check_in_time timestamp with time zone NOT NULL,
    check_out_time timestamp with time zone NULL,
    actual_hours numeric(5, 2) NULL,
    status varchar(50) NOT NULL DEFAULT 'PRESENT',
    notes text NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_time_attendances_emp_date ON time_attendances(emp_id, work_date);

-- 9. BẢNG NGHỈ PHÉP (LEAVE REQUESTS)
CREATE TABLE IF NOT EXISTS leave_requests (
    id uuid PRIMARY KEY,
    emp_id bigint NOT NULL REFERENCES employees(emp_id) ON DELETE CASCADE,
    leave_type varchar(50) NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    days_count numeric(4, 1) NOT NULL,
    reason text NOT NULL,
    status varchar(50) NOT NULL DEFAULT 'PENDING',
    rejection_reason text NULL,
    attachment_file_id uuid NULL,
    attachment_file_name text NULL,
    approved_by_user_id bigint NULL REFERENCES users(user_id) ON DELETE SET NULL,
    approved_at timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NULL
);

-- 10. BẢNG BẢNG LƯƠNG (PAYROLLS)
CREATE TABLE IF NOT EXISTS payrolls (
    payroll_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    emp_id bigint NOT NULL REFERENCES employees(emp_id) ON DELETE RESTRICT,
    period varchar(7) NOT NULL, -- YYYY-MM
    standard_working_days numeric(4,1) NOT NULL DEFAULT 22.0,
    actual_working_days numeric(4,1) NOT NULL DEFAULT 0,
    snapshot_base_salary numeric(15,2) NOT NULL DEFAULT 0,
    snapshot_position_allowance numeric(15,2) NOT NULL DEFAULT 0,
    snapshot_other_allowance numeric(15,2) NOT NULL DEFAULT 0,
    gross_salary numeric(15,2) NOT NULL DEFAULT 0,
    net_salary numeric(15,2) NOT NULL DEFAULT 0,
    status varchar(50) NOT NULL DEFAULT 'DRAFT',
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS ix_payrolls_emp_id ON payrolls(emp_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payrolls_emp_period ON payrolls(emp_id, period);

-- 11. BẢNG DÒNG THỜI GIAN NHÂN SỰ (TIMELINE)
CREATE TABLE IF NOT EXISTS employee_timeline_events (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    emp_id bigint NOT NULL REFERENCES employees(emp_id) ON DELETE CASCADE,
    event_type varchar(100) NOT NULL,
    title varchar(255) NOT NULL,
    description text NOT NULL,
    timestamp timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id bigint NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_by_name varchar(255) NULL
);

-- ==============================================================================
-- DỮ LIỆU SEED THỦ CÔNG CHUẨN (RUN CHỈ KHI CẦN THIẾT)
-- ==============================================================================

-- 1. CHÈN DANH MỤC 18 QUYỀN CHUẨN CỦA HỆ THỐNG
INSERT INTO permissions (perm_id, permission_code, description) VALUES
    (6, 'employee.read', 'Tra cứu Danh bạ Nhân sự'),
    (7, 'employee.write', 'Cập nhật Hồ sơ Nhân sự'),
    (8, 'employee.transfer', 'Điều chuyển phòng ban'),
    (9, 'employee.offboard', 'Thực hiện thôi việc'),
    (10, 'attendance.self.write', 'Chấm công vào/ra cá nhân'),
    (12, 'leave.self.create', 'Tạo đơn xin nghỉ phép'),
    (15, 'payroll.run', 'Tính & Chốt bảng lương'),
    (16, 'payroll.finalize', 'Duyệt chi lương chính thức'),
    (17, 'payroll.self.read', 'Xem phiếu lương cá nhân'),
    (18, 'report.read', 'Báo cáo & Thống kê Nhân sự'),
    (19, 'admin.user.read', 'Xem danh sách tài khoản'),
    (20, 'admin.user.search', 'Tìm kiếm nâng cao tài khoản'),
    (21, 'admin.user.manage', 'Quản lý tài khoản'),
    (22, 'admin.rbac.manage', 'Quản trị vai trò & quyền'),
    (23, 'audit.read', 'Nhật ký kiểm toán'),
    (25, 'leave.approve', 'Duyệt đơn nghỉ phép'),
    (27, 'employee.promote', 'Bổ nhiệm & Thăng chức'),
    (28, 'self_approved', 'Tự động duyệt đơn cá nhân')
ON CONFLICT (perm_id) DO UPDATE 
SET permission_code = EXCLUDED.permission_code, description = EXCLUDED.description;

-- 2. CHÈN CÁC VAI TRÒ CHUẨN
INSERT INTO roles (role_id, role_name, description) VALUES
    (1, 'ADMIN', 'Quản trị viên hệ thống (Tài khoản, phân quyền, kiểm toán)'),
    (2, 'HR_STAFF', 'Chuyên viên nhân sự (Hồ sơ, lương, phép, báo cáo)'),
    (3, 'MANAGER', 'Trưởng nhóm / Quản lý (Duyệt phép, duyệt chấm công)'),
    (4, 'EMPLOYEE', 'Nhân viên (Chấm công, nộp đơn, xem phiếu lương, danh bạ)')
ON CONFLICT (role_id) DO UPDATE
SET role_name = EXCLUDED.role_name, description = EXCLUDED.description;

-- 3. GÁN QUYỀN CHUẨN CHO TỪNG VAI TRÒ (Dựa theo permission_code để tránh lệch ID)

-- ADMIN: Chỉ 5 quyền quản trị & kiểm toán (Bảo vệ theo Mục 2 Rubric)
DELETE FROM role_permissions WHERE role_id = (SELECT role_id FROM roles WHERE upper(role_name) = 'ADMIN');
INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE upper(r.role_name) = 'ADMIN'
  AND p.permission_code IN ('admin.user.read', 'admin.user.search', 'admin.user.manage', 'admin.rbac.manage', 'audit.read')
ON CONFLICT DO NOTHING;

-- HR_STAFF: Toàn bộ quyền nghiệp vụ nhân sự, lương, phép, báo cáo, bổ nhiệm
DELETE FROM role_permissions WHERE role_id = (SELECT role_id FROM roles WHERE upper(role_name) = 'HR_STAFF');
INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE upper(r.role_name) = 'HR_STAFF'
  AND p.permission_code IN (
      'employee.read', 'employee.write', 'employee.transfer', 'employee.offboard', 'employee.promote',
      'attendance.self.write', 'leave.self.create', 'leave.approve',
      'payroll.run', 'payroll.finalize', 'payroll.self.read', 'report.read'
  )
ON CONFLICT DO NOTHING;

-- MANAGER: Quyền cá nhân + Duyệt phép + Bổ nhiệm
DELETE FROM role_permissions WHERE role_id = (SELECT role_id FROM roles WHERE upper(role_name) = 'MANAGER');
INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE upper(r.role_name) = 'MANAGER'
  AND p.permission_code IN (
      'employee.read', 'attendance.self.write', 'leave.self.create', 'leave.approve',
      'payroll.self.read', 'report.read', 'employee.promote'
  )
ON CONFLICT DO NOTHING;

-- EMPLOYEE: Quyền cá nhân tự phục vụ
DELETE FROM role_permissions WHERE role_id = (SELECT role_id FROM roles WHERE upper(role_name) = 'EMPLOYEE');
INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE upper(r.role_name) = 'EMPLOYEE'
  AND p.permission_code IN (
      'employee.read', 'attendance.self.write', 'leave.self.create', 'payroll.self.read'
  )
ON CONFLICT DO NOTHING;
