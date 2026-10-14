# ADR-016: Manage cloud database changes manually

Status: Accepted

Date: 2026-10-14

## Decision

Supabase is the cloud database. Schema changes and baseline data provisioning are performed manually by the operator in Supabase. This supersedes earlier requirements for application-managed schema migrations, startup initializers and automatic baseline seeding.

Do not add or run application schema initializers, `Database.Migrate`, `EnsureCreated`, automated schema/seed scripts, or deployment-time schema updates against Supabase. API startup only configures services and database mappings; it does not provision database objects or data. Health checks and cloud schema verification tests may report missing objects but must never repair them.

Versioned SQL migration files are allowed as reviewable artifacts for the operator to execute manually. `scripts/migrations/20261014_001_schema_and_seed.sql` consolidates the former module initializers and the manager/RBAC additions. There is no runner or startup/deployment integration. Demo payroll seeding and destructive legacy cleanup are optional switches, disabled by default; incompatible historical leave layouts require a separate manual data migration.

Normal business operations remain supported: creating/editing employees and accounts, role/permission assignment through authorized APIs, attendance, leave decisions, payroll calculations and audit writes.

Tests requiring table creation or fixture insertion must use an explicitly configured, separate disposable test database via `HRM_TEST_POSTGRES_CONNECTION`. They must not fall back to the application's cloud database connection. Temporary test tables are isolated to the test connection and disposed afterwards.

## Manual change workflow

1. Document the required tables, columns, data types, indexes, constraints and baseline permission codes with the feature change.
2. The operator reviews backup/rollback needs and applies the changes manually in Supabase.
3. Verify the resulting schema using read-only checks.
4. Deploy/run the matching application version and assign roles/managers through normal authorized business operations.

For ADR-015, provision nullable `employees.manager_id` as bigint, referencing `employees.emp_id` with restricted deletion, an index on manager_id, and a check preventing manager_id = emp_id. Add `leave_requests.is_auto_approved` as boolean NOT NULL with default false. Create permission code `self_approved` without default role grants. Historical requests retain false and existing employees retain a null manager until assigned.
