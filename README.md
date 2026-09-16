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

Store the Supabase PostgreSQL connection string in .NET user-secrets for the API project. Never use `SUPABASE_ANON_KEY` as the database password.

For the local `.env` workflow, copy `.env.example` to `.env`, fill in local values, and dot-source the loader before running .NET commands:

```powershell
. .\scripts\load-env.ps1
```

The root `.env` is ignored by Git. `.NET` does not load `.env` automatically, so the loader must run in each new PowerShell terminal. User-secrets remain the recommended alternative for the backend database password.

```powershell
dotnet user-secrets init --project apps/api/Hrm.Api/Hrm.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=5432;Database=postgres;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true" --project apps/api/Hrm.Api/Hrm.Api.csproj
```

Run API and frontend in two terminals:

```powershell
dotnet run --project apps/api/Hrm.Api/Hrm.Api.csproj --launch-profile https
dotnet run --project apps/web/Hrm.Web/Hrm.Web.csproj --launch-profile https
```

- Frontend: `https://localhost:7100`
- API: `https://localhost:7060`
- Liveness: `https://localhost:7060/health/live`
- Readiness (includes PostgreSQL): `https://localhost:7060/health/ready`
- OpenAPI JSON (Development): `https://localhost:7060/openapi/v1.json`

Apply the committed migration only after configuring the backend connection string:

```powershell
dotnet tool run dotnet-ef database update --project apps/api/Modules/Auth/Hrm.Modules.Auth.csproj --startup-project apps/api/Hrm.Api/Hrm.Api.csproj --context AuthDbContext
```

Run the automated checks using the .NET 10 Microsoft Testing Platform syntax:

```powershell
dotnet test --solution Hrm.slnx
```
