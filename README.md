# HRM System — Saigon Retail JSC

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
- Core HR (Employees, Departments, Positions)
- Attendance & Leave
- Payroll
- Reports & Analytics
- Employee Self-Service (ESS)
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

In Development, API startup uses non-destructive `CREATE TABLE IF NOT EXISTS` initialization for the Milestone 2 support tables (`refresh_tokens`, `user_security_states`, `user_account_metadata`, `audit_logs`). Do not run the removed ASP.NET Identity migration against the existing Supabase legacy schema.

Run the automated checks using the .NET 10 Microsoft Testing Platform syntax:

```powershell
.\scripts\test-all.ps1
```
