# Database Design Baseline

## Tables từ PRD
- Departments
- Positions
- Employees
- Users
- Roles
- Permissions
- RolePermissions
- TimeAttendances
- LeaveRequests
- Payrolls

## Bổ sung kỹ thuật đề xuất
- RefreshTokens
- AuditLogs
- Attachments
- PayrollAdjustments
- ApprovalHistories
- BackupJobs

## Lưu ý
Các bảng bổ sung là quyết định triển khai, không phải nội dung nguyên bản của PRD.

## Tables bổ sung cho Retail Modules

### Products
- ProductId
- ProductCode
- ProductName
- Category
- Unit
- Status
- SupplierId? (optional baseline)
- CreatedAt
- UpdatedAt

### Suppliers
- SupplierId
- SupplierCode
- SupplierName
- ContactName
- Phone
- Email
- Address
- Status
- CreatedAt
- UpdatedAt

### Warehouses
- WarehouseId
- WarehouseCode
- WarehouseName
- Address
- Status

### Inventories
- InventoryId
- WarehouseId
- ProductId
- QuantityOnHand
- ReorderLevel
- UpdatedAt

Unique baseline:
- WarehouseId + ProductId

### StockMovements
- StockMovementId
- WarehouseId
- ProductId
- MovementType
- Quantity
- ReferenceType
- ReferenceId
- Status
- CreatedBy
- CreatedAt

### PurchaseOrders
- PurchaseOrderId
- SupplierId
- OrderDate
- Status
- TotalAmount

### PurchaseOrderItems
- PurchaseOrderItemId
- PurchaseOrderId
- ProductId
- Quantity
- UnitCost

### SalesOrders
- SalesOrderId
- OrderDate
- Status
- TotalAmount

### SalesOrderItems
- SalesOrderItemId
- SalesOrderId
- ProductId
- Quantity
- UnitPrice

## PostgreSQL / Supabase implementation notes
- .NET `Guid` maps to PostgreSQL `uuid` through Npgsql.
- EF Core provider is Npgsql.
- Configure EF Core mappings to match the manually managed Supabase schema. The operator creates/changes PostgreSQL indexes and constraints manually (ADR-016); application startup and deployment do not run schema migrations or seed data.
- Supabase API/Auth/Storage are optional platform capabilities and require explicit tasks/ADRs before adoption.
