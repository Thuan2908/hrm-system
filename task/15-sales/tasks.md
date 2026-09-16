# Epic 15 — Sales

## SAL-001 — Sales Order model
**Mục tiêu:** Tạo SalesOrder/SalesOrderItem.

**Acceptance Criteria:**
- At least one item
- Valid product
- Statuses

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SAL-002 — Create sales order
**Mục tiêu:** Tạo đơn bán hàng.

**Acceptance Criteria:**
- Calculate total
- Authorization
- Validation

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SAL-003 — Confirm sales order
**Mục tiêu:** Xác nhận đơn.

**Acceptance Criteria:**
- State transition
- Audit
- Permission

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SAL-004 — Inventory issue integration
**Mục tiêu:** Xuất kho khi bán.

**Acceptance Criteria:**
- Call Inventory contract
- No direct DB access
- Atomic orchestration strategy

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SAL-005 — Sales product search
**Mục tiêu:** Tìm/lọc/sắp xếp sản phẩm.

**Acceptance Criteria:**
- Code/name
- Category/status
- Name/CreatedAt sort

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SAL-006 — Sales order search
**Mục tiêu:** Tìm/lọc đơn hàng.

**Acceptance Criteria:**
- Date/status
- Pagination
- Sort

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SAL-007 — Sales report baseline
**Mục tiêu:** Báo cáo bán hàng.

**Acceptance Criteria:**
- Revenue summary baseline
- Top products baseline
- Permission

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## SAL-008 — Sales UI
**Mục tiêu:** Tạo giao diện bán hàng.

**Acceptance Criteria:**
- Dashboard
- Products
- Orders
- Reports
- Tách admin layout

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
