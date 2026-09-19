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

## Reports
Read models and analytics by authorized domain.

## Audit
Audit trail cross-cutting.

## Files
Attachment metadata and storage contract.

### Forbidden
- payroll sửa employee trực tiếp
- reports sửa domain data
- frontend quyết định authorization thay backend
