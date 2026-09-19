# Epic 11 — Admin Advanced Management

## ADM-001 — Admin UI isolation
**Mục tiêu:** Tách hoàn toàn Admin UI khỏi HR và Employee UI.

**Acceptance Criteria:**
- Route `/admin/*` riêng
- Layout/navigation riêng
- Guard riêng
- Không dùng dashboard nghiệp vụ

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ADM-002 — User basic search
**Mục tiêu:** Tìm account theo full name/username.

**Acceptance Criteria:**
- Search partial match
- Pagination
- Permission `admin.user.search`

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ADM-003 — User advanced filters
**Mục tiêu:** Lọc account đa tiêu chí.

**Acceptance Criteria:**
- Department
- Role
- Status
- Có thể kết hợp nhiều filter

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ADM-004 — User sorting
**Mục tiêu:** Sắp xếp danh sách account.

**Acceptance Criteria:**
- CreatedAt asc/desc
- LastLogin asc/desc
- Username asc/desc

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ADM-005 — Account lifecycle
**Mục tiêu:** Quản lý vòng đời account.

**Acceptance Criteria:**
- Create
- Lock/Unlock
- Deactivate
- Reset password
- Không hard delete

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ADM-006 — RBAC administration
**Mục tiêu:** Gán và thu hồi quyền.

**Acceptance Criteria:**
- Assign/revoke role
- Assign/revoke permission
- Audit mọi thay đổi

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
