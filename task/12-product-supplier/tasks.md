# Epic 12 — Product & Supplier

## CAT-001 — Product model
**Mục tiêu:** Tạo entity/schema Product.

**Acceptance Criteria:**
- Unique ProductCode
- Status Active/Inactive
- Audit fields

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-002 — Product CRUD
**Mục tiêu:** Quản lý sản phẩm.

**Acceptance Criteria:**
- Create/read/update/deactivate
- Không hard delete nếu có lịch sử
- Authorization

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-003 — Product search
**Mục tiêu:** Tìm kiếm sản phẩm.

**Acceptance Criteria:**
- Search code/name
- Pagination
- Case-insensitive phù hợp

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-004 — Product filter
**Mục tiêu:** Lọc sản phẩm.

**Acceptance Criteria:**
- Category
- Status
- Supplier baseline

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-005 — Product sort
**Mục tiêu:** Sắp xếp sản phẩm.

**Acceptance Criteria:**
- Name
- CreatedAt
- UpdatedAt

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-006 — Supplier model
**Mục tiêu:** Tạo entity/schema Supplier.

**Acceptance Criteria:**
- Unique SupplierCode
- Contact fields
- Status

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-007 — Supplier CRUD
**Mục tiêu:** Quản lý nhà cung cấp.

**Acceptance Criteria:**
- Create/read/update/deactivate
- Authorization
- Audit

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-008 — Supplier search
**Mục tiêu:** Tìm kiếm nhà cung cấp.

**Acceptance Criteria:**
- Code/name/contact
- Pagination
- Permission

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-009 — Supplier filter/sort
**Mục tiêu:** Lọc/sắp xếp supplier.

**Acceptance Criteria:**
- Status filter
- Name/CreatedAt sort

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## CAT-010 — Catalog UI
**Mục tiêu:** Xây UI Product/Supplier.

**Acceptance Criteria:**
- Sales product screen
- Procurement supplier screen
- Không dùng admin layout

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
