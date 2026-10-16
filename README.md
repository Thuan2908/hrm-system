# Saigon Retail Management System

## Architecture
- Monorepo
- Modular Monolith

## Stack
- Frontend: Blazor WebAssembly + C#
- Backend: ASP.NET Core Web API + C#
- ORM: Entity Framework Core + Npgsql
- Database: PostgreSQL hosted on Supabase

## Recommended flow

```text
Blazor WebAssembly
        ↓
ASP.NET Core Web API
        ↓
EF Core + Npgsql
        ↓
Supabase PostgreSQL
```

## Supabase public/client configuration

```text
SUPABASE_URL=https://xrieskmmmlgtonltuina.supabase.co
SUPABASE_ANON_KEY=<provided anon key>
```

The anon key is not the database password.

Backend uses a separate secret connection string.

## Domains
- Admin/RBAC
- HRM
- Products
- Suppliers
- Warehouse/Inventory
- Procurement
- Sales
- Reports
- Audit

## Development references

- Full implementation prompt: `docs/project-build-prompt.md`
- .NET dependency requirements: `requirements.md`
- Central NuGet versions: `Directory.Packages.props`

## Run locally

Prerequisites: .NET SDK version pinned by `global.json`; Docker is optional until PostgreSQL integration tests are enabled.

```powershell
dotnet tool restore
dotnet restore Hrm.slnx
dotnet build Hrm.slnx --no-restore
```

For this local workspace, the Supabase PostgreSQL connection string and development JWT key are loaded from `apps/api/Hrm.Api/appsettings.Development.Local.json`. This file is ignored by Git. Never use `SUPABASE_ANON_KEY` as the database password.

For the local `.env` workflow, copy `.env.example` to `.env`, fill in local values, and dot-source the loader before running .NET commands:

```powershell
. .\scripts\load-env.ps1
```

The root `.env` is ignored by Git. `.NET` does not load `.env` automatically, so the loader must run in each new PowerShell terminal. User-secrets remain an optional alternative for another developer or machine.

```powershell
dotnet user-secrets init --project apps/api/Hrm.Api/Hrm.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=5432;Database=postgres;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true" --project apps/api/Hrm.Api/Hrm.Api.csproj
```

### Quick run (Single command)

Run both API and Frontend concurrently in one terminal from the workspace root:

```powershell
dotnet run
```

### Or run separately in two terminals:

```powershell
dotnet run --project apps/api/Hrm.Api/Hrm.Api.csproj --launch-profile https
dotnet run --project apps/web/Hrm.Web/Hrm.Web.csproj --launch-profile https
```

- Frontend: `https://localhost:7100`
- API: `https://localhost:7060`
- Liveness: `https://localhost:7060/health/live`
- Readiness (includes PostgreSQL): `https://localhost:7060/health/ready`
- OpenAPI JSON (Development): `https://localhost:7060/openapi/v1.json`

Supabase schema and baseline data are managed manually. API startup does not create, alter, drop or seed database objects, and there is no automatic migration step. Provision required tables, columns, indexes, constraints and baseline permissions in Supabase before running features that depend on them. See `docs/decisions/ADR-016-manual-cloud-database-changes.md`.

The single manual schema/seed file is `scripts/migrations/20261014_001_schema_and_seed.sql`. Run the entire file manually in Supabase SQL Editor for a new database or a compatible existing HRM database. It replaces the old root-level bootstrap and separate permission seed, and includes all current modules, employee/position fields, manager approval and `leave.approve.all`. Existing role IDs and grants are retained; default grants are assigned only when a baseline role is first created. ADMIN starts with account/RBAC/audit permissions only. No accounts or passwords are seeded. Optional demo payroll and destructive cleanup switches are false by default. Incompatible historical payroll/leave layouts stop execution and require a separate reviewed data conversion. No application command runs this file.

For manager-based leave approval, manually add nullable `employees.manager_id` (`bigint`, referencing `employees.emp_id`, indexed, and disallowing self-management), `leave_requests.is_auto_approved` (`boolean`, NOT NULL, default false), and the `self_approved` entry in `permissions`. Grant it only to the intended roles using role management. Existing employees need an assigned manager and that manager's role needs `leave.approve` for manual reviews.

Cloud schema verification tests are read-only. Workflow tests that create temporary tables run only when `HRM_TEST_POSTGRES_CONNECTION` points to a separate disposable test database; they do not use the application's Supabase connection.

For system-wide leave approval, the consolidated file creates `leave.approve.all`; explicitly grant it to the intended roles in Admin role management. No role receives this permission automatically. Refresh the affected session to update navigation. `leave.approve` retains direct-manager scope; global reviewers can filter departments and sort by submission time or full name on the same approval page. Manual self-approval remains prohibited.

Run the automated checks using the .NET 10 Microsoft Testing Platform syntax:

```powershell
.\scripts\test-all.ps1
```
