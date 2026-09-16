# Module Boundaries

## Auth / Users
Users, Roles, Permissions, token, password, account lifecycle.

## Employees
Employee profile, department, position, lifecycle.

## Attendance
Check-in/out, actual hours, OT, timesheet closing.

## Leave
Leave request, approval workflow.

## Payroll
Salary calculation, deductions, payslip, finalize, bank export.

## Products
Product master data, product search/filter/sort.

## Suppliers
Supplier master data, supplier search/filter/sort.

## Warehouses
Warehouse master data.

## Inventory
Stock balance, stock movements, warehouse-product quantity.

## Procurement
Purchase order and goods receipt baseline.

## Sales
Sales order and sales reporting baseline.

## Reports
Read models and analytics by authorized domain.

## Audit
Audit trail cross-cutting.

## Files
Attachment metadata and storage contract.

### Forbidden
- payroll sửa employee trực tiếp
- reports sửa domain data
- sales ghi thẳng inventory table
- procurement ghi thẳng inventory table
- warehouse/sales UI dùng admin layout
- frontend quyết định authorization thay backend
