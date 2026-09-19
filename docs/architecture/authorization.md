# Authorization

Mô hình RBAC:
User -> Role -> Permissions.

Permission code gợi ý:
- employee.read
- employee.write
- employee.transfer
- employee.offboard
- attendance.self.write
- attendance.team.approve
- leave.self.create
- leave.team.approve
- leave.hr.approve
- payroll.run
- payroll.finalize
- payroll.self.read
- report.read
- admin.user.manage
- admin.rbac.manage
- audit.read

## Retail Permission Codes

### Product
- product.read
- product.create
- product.update
- product.deactivate

### Supplier
- supplier.read
- supplier.create
- supplier.update
- supplier.deactivate

### Warehouse
- warehouse.read
- warehouse.manage

### Inventory
- inventory.read
- inventory.adjust
- inventory.transfer
- inventory.post

### Procurement
- procurement.read
- procurement.create
- procurement.approve
- procurement.receive

### Sales
- sales.read
- sales.create
- sales.update
- sales.confirm
- sales.report.read

### Admin advanced search
- admin.user.read
- admin.user.search
- admin.user.manage
- admin.rbac.manage
