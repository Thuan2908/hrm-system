# Service Contracts

Ví dụ contract cross-module:

## EmployeeQueryService
- getEmployeePayrollProfile(employeeId)
- getEmployeeStatus(employeeId)
- getManagerOf(employeeId)

## AttendanceQueryService
- getClosedTimesheet(employeeId, period)

## LeaveQueryService
- getApprovedLeaveForPeriod(employeeId, period)

## PayrollCommandService
- calculatePeriod(period)
- finalizePayroll(payrollId)

Không module nào được bypass contract để đọc/ghi repository nội bộ module khác.

## ProductQueryService
- GetProduct(productId)
- SearchProducts(criteria)

## SupplierQueryService
- GetSupplier(supplierId)
- SearchSuppliers(criteria)

## InventoryService
- GetStock(warehouseId, productId)
- ReceiveStock(command)
- IssueStock(command)
- TransferStock(command)

## ProcurementService
- CreatePurchaseOrder(command)
- ReceiveGoods(command)

## SalesService
- CreateSalesOrder(command)
- ConfirmSalesOrder(command)
