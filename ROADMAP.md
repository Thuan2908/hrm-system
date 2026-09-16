# ROADMAP.md

## Phase 0 — Foundation
Repository, conventions, environment, CI skeleton, schema baseline.

## Phase 1 — Security & Admin
Authentication, RBAC, Users, Admin console, Audit log.

## Phase 2 — Core HR
Employees, Departments, Positions, onboarding, transfer, offboarding.

## Phase 3 — Attendance & Leave
Check-in/out, OT, online leave request, 2-level approval.

## Phase 4 — Payroll
Timesheet closing, salary formula, deductions, payslip, bank export.

## Phase 5 — Reporting & ESS
Real-time dashboard, HR analytics, employee self-service, print/export.

## Phase 6 — Data Migration & Operations
Excel migration, backup, restore, monitoring, disaster recovery.

## Phase 7 — Hardening & Product
Security hardening, UAT, documentation, deployment, handover.

## Phase 8 — Retail Expansion
Products, Suppliers, Warehouse, Inventory, Procurement và Sales.

## Phase 9 — Cross-domain Hardening
Permission matrix mở rộng, transaction boundaries, reporting integration và UAT liên phân hệ.

## Architecture Migration — Blazor + Supabase
- Replace previous frontend stack with Blazor WebAssembly.
- Replace SQL Server with PostgreSQL hosted on Supabase.
- Use Npgsql for EF Core.
- Add Supabase environment/secrets configuration.
