# ADR-015: Direct manager leave approval and permission-based automatic approval

Status: Accepted

## Decision

Employees have a nullable `manager_id` referencing `employees.emp_id`. Self-management and management cycles are rejected. Existing employees keep a null manager until assigned through employee management.

For ordinary leave, the explicit `self_approved` permission on the submitting user's role causes automatic approval at submission, with actor, timestamp, automatic flag and audit. No role receives this permission automatically. Resignations always require manual approval and keep the existing offboarding behavior.

Otherwise, only the employee's current direct manager with `leave.approve` (or its supported aliases) may view the pending queue, approve, or reject. Active employee/account and lock status are checked from the database on each operation. Admin role alone does not override these restrictions. Managers cannot manually approve/reject their own requests.

This replaces the earlier two-stage Leader → HR approval baseline by the product request of 2026-10-14. `self_approved` is a permission assigned to roles, not a new role.

## Consequences

Managers without approval permission and employees without managers cannot have pending requests reviewed until assignments/grants are corrected. Changing an employee's manager transfers their pending requests to the new manager. Existing pending requests are not retroactively auto-approved. Automatic approvals cannot be edited or cancelled through pending-only actions.

Per ADR-016, the operator manually provisions the manager column, foreign key, self-management check and index, adds `leave_requests.is_auto_approved` (boolean, NOT NULL, default false), and creates the `self_approved` permission in Supabase. The Auth permission catalog describes permissions for application/UI behavior; it does not seed database rows.
