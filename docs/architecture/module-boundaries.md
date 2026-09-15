# Module Boundaries

## auth
Users, Roles, Permissions, token, password.

## employees
Employee profile, department, position, lifecycle.

## attendance
Check-in/out, actual hours, OT, timesheet closing.

## leave
Leave request, approval workflow.

## payroll
Salary calculation, deductions, payslip, finalize, bank export.

## reports
Read models và analytics.

## audit
Audit trail cross-cutting.

## files
Attachment metadata và storage contract.

### Forbidden
- payroll sửa employee trực tiếp;
- reports sửa payroll;
- leave truy cập raw users repository;
- frontend quyết định authorization thay backend.
