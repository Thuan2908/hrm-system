# Product Scope

Hệ thống được mở rộng từ HRM thành **Saigon Retail Management System** theo kiến trúc Modular Monolith, trong đó HRM vẫn là phân hệ trọng tâm nhưng có thêm Inventory, Procurement và Sales.

## In Scope

### 1. Admin & Security
- Authentication
- Account management
- RBAC
- Admin UI riêng biệt
- Advanced search/filter/sort
- Audit log
- Backup/restore administration

### 2. HRM
- Employee profile
- Department
- Position
- Attendance & OT
- Leave / sickness / maternity / resignation requests
- Payroll & payslip
- HR reports
- Employee Self-Service

### 3. Warehouse & Inventory
- Warehouse management
- Stock management
- Stock movement
- Inventory search/filter/sort
- Low-stock visibility

### 4. Procurement & Supplier
- Supplier management
- Supplier search/filter/sort
- Purchase Order baseline
- Goods receipt baseline

### 5. Product & Sales
- Product catalog
- Product search/filter/sort
- Sales order baseline
- Sales management screens
- Sales reporting baseline

## Out of Scope Baseline
- AI attrition prediction
- FaceID mobile attendance
- Full CRM
- Full accounting/general ledger
- Marketplace/e-commerce integration
- Advanced forecasting

Các chức năng ngoài scope chỉ được thêm qua Change Request/ADR.
