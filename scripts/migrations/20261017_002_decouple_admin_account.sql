-- ============================================================================
-- Migration: 20261017_002_decouple_admin_account.sql
-- Mục đích: Tách biệt tài khoản Quản trị Hệ thống (System Administrator) khỏi
--           hồ sơ nhân sự nghiệp vụ (Employee).
-- Nguyên tắc: Admin hệ thống là tài khoản kỹ thuật của Chủ hệ thống (Root/Owner),
--             không đại diện cho bất kỳ nhân viên nào trong danh bạ nhân sự.
-- ============================================================================

-- 1. Đảm bảo ràng buộc FK trên bảng users cho phép NULL (mặc định đã là bigint NULL)
ALTER TABLE users ALTER COLUMN emp_id DROP NOT NULL;

-- 2. Tách tài khoản Admin khỏi bất kỳ nhân viên nào hiện tại
UPDATE users
SET emp_id = NULL
WHERE LOWER(username) IN ('admin', 'admin_root')
   OR role_id IN (SELECT role_id FROM roles WHERE UPPER(role_name) = 'ADMIN');

-- 3. Ghi nhận kiểm toán nếu cần
DO $$
BEGIN
    RAISE NOTICE 'Đã tách biệt tài khoản Quản trị Hệ thống khỏi hồ sơ nhân sự thành công.';
END $$;
