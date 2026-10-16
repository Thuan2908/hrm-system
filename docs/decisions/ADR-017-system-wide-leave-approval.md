# ADR-017: Separate direct-manager and system-wide leave approval

Status: Accepted

Date: 2026-10-16

## Decision

Keep `employees.manager_id` and the existing `leave.approve` permission for direct-manager review. Introduce the separate `leave.approve.all` permission for system-wide listing, approval and rejection, including employees without a manager. Role names and positions do not implicitly grant this permission, including ADMIN. If both grants exist, global scope applies. Existing aliases such as `leave.hr.approve` retain direct-manager scope.

The authenticated API delegates final authorization to Leave's application service, which checks current active/lock status and database role grants on every review/options request. No manual self-approval is permitted in either scope. `self_approved` remains an independent permission for ordinary leave only; resignations still require manual approval and keep their existing offboarding behavior.

The same approval page serves both scopes. Global reviewers can filter departments and order by submission time or full name in either direction. Server filtering always occurs within authorized scope. Sort keys are allowlisted; full names use Vietnamese culture comparison and request ID breaks ties. Department options come from the authorized scope and do not require separate employee directory permissions.

Leave status is an optimistic concurrency token. A stale competing decision receives HTTP 409 and cannot overwrite the first saved transition. Resignation side effects occur only after winning the pending-state transition. Existing audit records preserve actor and decision. Global reviewers may read attachments linked to other employees' leave requests without receiving blanket access to other uploaded files.

## Manual database change

Run the single `scripts/migrations/20261014_001_schema_and_seed.sql` schema/seed file manually in Supabase SQL Editor, then explicitly grant the permission to the intended HR/CEO roles through Admin role management. The separate permission seed has been consolidated into this file. Neither `leave.approve.all` nor `self_approved` receives automatic role grants. Refresh affected sessions to update menu claims. Revoking the grant takes effect for backend authorization immediately.

This permission requires no new schema/table and no automatic migration execution. The consolidated baseline preserves existing role grants and seeds defaults only for newly created roles; new ADMIN roles receive account/RBAC/audit permissions only.

## Limits

The pending-list API retains its current list response; it filters and sorts on the server but does not add pagination in this change. A separate paged read model can be added if pending volume requires it. This introduces two permission scopes, not a second sequential HR approval stage.
