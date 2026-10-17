-- Migration: Streamline HR permissions to 3 standard codes (employee.read, employee.create, employee.update)

-- 1. Reset sequence để tránh lỗi duplicate key nếu sequence bị lệch
SELECT setval(pg_get_serial_sequence('permissions', 'perm_id'), COALESCE(MAX(perm_id), 1)) FROM permissions;

-- 2. Thêm 2 mã quyền chuẩn vào bảng permissions
DO $permissions$
DECLARE
    r RECORD;
    next_id bigint;
BEGIN
    FOR r IN SELECT * FROM (VALUES
        ('employee.create', 'Thêm nhân sự mới (Onboarding)'),
        ('employee.update', 'Cập nhật hồ sơ & Điều hành')
    ) AS t(code, descr)
    LOOP
        IF NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = r.code) THEN
            SELECT COALESCE(MAX(perm_id), 0) + 1 INTO next_id FROM permissions;
            INSERT INTO permissions (perm_id, permission_code, description)
            VALUES (next_id, r.code, r.descr);
        ELSE
            UPDATE permissions SET description = r.descr WHERE permission_code = r.code;
        END IF;
    END LOOP;
END $permissions$;

-- 3. Cấp bổ sung 2 quyền mới này cho vai trò HR_STAFF (nếu vai trò đang tồn tại)
INSERT INTO role_permissions (role_id, perm_id)
SELECT r.role_id, p.perm_id
FROM roles r
CROSS JOIN permissions p
WHERE UPPER(r.role_name) = 'HR_STAFF'
  AND p.permission_code IN ('employee.create', 'employee.update')
ON CONFLICT DO NOTHING;

-- 4. Đồng bộ lại sequence
SELECT setval(pg_get_serial_sequence('permissions', 'perm_id'), COALESCE(MAX(perm_id), 1)) FROM permissions;
