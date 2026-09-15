# Epic 03 — Core HR

## HR-001 — Department CRUD
**Mục tiêu:** Quản lý phòng ban/chi nhánh.

**Acceptance Criteria:**
- Unique code
- Search/filter
- Không xóa cứng nếu đang được tham chiếu

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-002 — Position CRUD
**Mục tiêu:** Quản lý chức danh/phụ cấp.

**Acceptance Criteria:**
- Tên/chức danh hợp lệ
- Allowance baseline lưu được
- Permission phù hợp

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-003 — Employee create
**Mục tiêu:** Onboarding nhân viên.

**Acceptance Criteria:**
- Sinh emp_code duy nhất
- Gán department/position
- Tạo user khi đủ điều kiện

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-004 — Employee update
**Mục tiêu:** Cập nhật hồ sơ.

**Acceptance Criteria:**
- HR sửa trường quản trị
- Employee chỉ sửa field được phép
- Audit field nhạy cảm

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-005 — Employee detail
**Mục tiêu:** Xem hồ sơ đầy đủ.

**Acceptance Criteria:**
- Role-based visibility
- Hiển thị lifecycle/status
- Không lộ dữ liệu trái quyền

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-006 — Transfer
**Mục tiêu:** Điều chuyển phòng ban.

**Acceptance Criteria:**
- Cập nhật department
- Lưu lịch sử thay đổi
- Không tạo duplicate active assignment

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-007 — Promotion
**Mục tiêu:** Bổ nhiệm chức danh.

**Acceptance Criteria:**
- Cập nhật position
- Cập nhật quyền liên quan
- Audit quyết định

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-008 — Offboarding
**Mục tiêu:** Thôi việc an toàn.

**Acceptance Criteria:**
- Status RESIGNED
- Khóa user
- Không hard delete

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-009 — Employee search
**Mục tiêu:** Tìm kiếm đa tiêu chí.

**Acceptance Criteria:**
- Tên/mã/department/status
- Pagination
- Sort cơ bản

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## HR-010 — Profile history
**Mục tiêu:** Theo dõi thay đổi hồ sơ.

**Acceptance Criteria:**
- Có timeline sự kiện chính
- Audit liên kết entity
- Chỉ người có quyền xem

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
