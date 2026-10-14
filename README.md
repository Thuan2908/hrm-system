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

The consolidated manual migration is `scripts/migrations/20261014_001_schema_and_seed.sql`. Open the file and run it manually in Supabase SQL Editor against the existing legacy database. It contains Auth, Employees, Files, Attendance, Leave and Payroll schema/seed SQL, including the manager/RBAC additions. The optional demo payroll and destructive cleanup switches at the top are false by default. No application command runs this file.

For manager-based leave approval, manually add nullable `employees.manager_id` (`bigint`, referencing `employees.emp_id`, indexed, and disallowing self-management), `leave_requests.is_auto_approved` (`boolean`, NOT NULL, default false), and the `self_approved` entry in `permissions`. Grant it only to the intended roles using role management. Existing employees need an assigned manager and that manager's role needs `leave.approve` for manual reviews.

Cloud schema verification tests are read-only. Workflow tests that create temporary tables run only when `HRM_TEST_POSTGRES_CONNECTION` points to a separate disposable test database; they do not use the application's Supabase connection.

Run the automated checks using the .NET 10 Microsoft Testing Platform syntax:

```powershell
.\scripts\test-all.ps1
```
