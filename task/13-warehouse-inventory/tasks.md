# Epic 13 — Warehouse & Inventory

## INV-001 — Warehouse model
**Mục tiêu:** Tạo Warehouse.

**Acceptance Criteria:**
- Unique WarehouseCode
- Address
- Status

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-002 — Warehouse CRUD
**Mục tiêu:** Quản lý kho.

**Acceptance Criteria:**
- Create/read/update/deactivate
- Permission warehouse.manage

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-003 — Inventory balance
**Mục tiêu:** Quản lý tồn kho.

**Acceptance Criteria:**
- Unique Warehouse+Product
- QuantityOnHand
- ReorderLevel

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-004 — Stock receive
**Mục tiêu:** Nhập kho.

**Acceptance Criteria:**
- Tạo StockMovement
- Tăng quantity
- Atomic transaction

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-005 — Stock issue
**Mục tiêu:** Xuất kho.

**Acceptance Criteria:**
- Tạo StockMovement
- Không âm tồn nếu không được phép
- Atomic transaction

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-006 — Stock transfer
**Mục tiêu:** Chuyển kho.

**Acceptance Criteria:**
- Giảm kho nguồn
- Tăng kho đích
- Một transaction logic

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-007 — Inventory search
**Mục tiêu:** Tìm kiếm tồn kho.

**Acceptance Criteria:**
- Product code/name
- Warehouse
- Pagination

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-008 — Inventory filter/sort
**Mục tiêu:** Lọc/sắp xếp tồn.

**Acceptance Criteria:**
- Category/status/low-stock
- Quantity/name sort

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-009 — Inventory audit
**Mục tiêu:** Lưu lịch sử điều chỉnh.

**Acceptance Criteria:**
- Actor
- Reason
- Before/after
- Không sửa posted movement

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## INV-010 — Warehouse UI
**Mục tiêu:** Tạo UI quản lý kho.

**Acceptance Criteria:**
- Dashboard
- Inventory list
- Movements
- Transfers

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
