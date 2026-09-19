# ADR-011: Expand Beyond HRM Into Retail Operations

Status: Accepted

## Context
Requirement mới bổ sung Product, Supplier, Warehouse/Inventory và Sales management.

## Decision
Giữ nguyên Monorepo + Modular Monolith và bổ sung các module:
- Products
- Suppliers
- Warehouses
- Inventory
- Procurement
- Sales

## Consequences
- Hệ thống trở thành enterprise retail management system thay vì HRM thuần.
- Cần RBAC và UI shell riêng cho từng nhóm nghiệp vụ.
- Cần kiểm soát transaction giữa Sales/Procurement và Inventory.
