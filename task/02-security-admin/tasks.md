# Epic 02 — Security & Admin

## SEC-001 — Login
**Mục tiêu:** Đăng nhập bằng username/password.

**Acceptance Criteria:**
- Token phát hành đúng
- Sai mật khẩu trả lỗi an toàn
- last_login được cập nhật

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SEC-002 — Refresh token
**Mục tiêu:** Duy trì session an toàn.

**Acceptance Criteria:**
- Có rotation
- Token bị revoke khi logout
- Không log token

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SEC-003 — RBAC model
**Mục tiêu:** Role-Permission backend.

**Acceptance Criteria:**
- Seed role baseline
- Guard permission hoạt động
- Default deny

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SEC-004 — Admin user management
**Mục tiêu:** Quản lý account.

**Acceptance Criteria:**
- Create/reset/lock/unlock user
- Search/filter theo role/status
- Audit đầy đủ

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SEC-005 — Role management
**Mục tiêu:** Quản lý role và permission.

**Acceptance Criteria:**
- Gán/bỏ permission
- Không tạo duplicate code
- Thay đổi có audit

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SEC-006 — Admin separation
**Mục tiêu:** Tách admin UI.

**Acceptance Criteria:**
- Route /admin/* riêng
- Guard riêng
- Không dùng chung navigation employee

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SEC-007 — Audit log
**Mục tiêu:** Ghi log thao tác nhạy cảm.

**Acceptance Criteria:**
- Actor/action/time/entity lưu đủ
- Có filter cơ bản
- Không log secret

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SEC-008 — Offboarding access revoke
**Mục tiêu:** Khóa truy cập khi thôi việc.

**Acceptance Criteria:**
- Employee RESIGNED -> account inactive
- Refresh token bị revoke
- Có audit

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
