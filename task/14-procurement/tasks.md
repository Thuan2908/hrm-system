# Epic 14 — Procurement

## PUR-001 — Purchase Order model
**Mục tiêu:** Tạo PO/PO items.

**Acceptance Criteria:**
- Supplier required
- At least one item
- Statuses

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PUR-002 — Create PO
**Mục tiêu:** Tạo đơn mua hàng.

**Acceptance Criteria:**
- Valid supplier
- Valid products
- Calculate total

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PUR-003 — Approve PO
**Mục tiêu:** Duyệt PO.

**Acceptance Criteria:**
- Permission procurement.approve
- Audit
- State transition

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PUR-004 — Goods receipt
**Mục tiêu:** Nhận hàng.

**Acceptance Criteria:**
- Reference PO
- Create stock receive movements
- Atomic

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PUR-005 — Procurement search
**Mục tiêu:** Tìm/lọc PO.

**Acceptance Criteria:**
- Supplier/status/date
- Pagination
- Sort

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PUR-006 — Procurement UI
**Mục tiêu:** Xây UI mua hàng.

**Acceptance Criteria:**
- Suppliers
- PO list/detail
- Goods receipt

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
