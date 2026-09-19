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
