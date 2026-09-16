# ARCHITECTURE.md

## 1. Kiến trúc cấp cao
Dự án dùng **Monorepo + Modular Monolith**.

```text
hrm-system/
├── apps/
│   ├── web        # .NET Blazor WebAssembly + C#
│   └── api        # ASP.NET Core Web API + C#
├── shared/
│   ├── Contracts/
│   ├── SharedKernel/
│   └── UI/
├── docs/
├── .agent/
└── task/
```

## 2. Technology Stack

### Frontend
- .NET Blazor WebAssembly
- C#
- Razor Components
- Typed HttpClient services

### Backend
- ASP.NET Core Web API
- C#
- Entity Framework Core
- Npgsql provider
- ASP.NET Core Authentication / Authorization

### Database / Platform
- PostgreSQL hosted on Supabase

## 3. Supabase configuration model

`SUPABASE_URL` and `SUPABASE_ANON_KEY` are API/client configuration values.

They are **not** the PostgreSQL database username/password or EF Core connection string.

Backend persistence uses a separate secret:

```text
ConnectionStrings__DefaultConnection
```

The connection string must come from the Supabase Database connection settings and must not be committed to Git.

Never expose:
- PostgreSQL password
- Supabase service-role key
- JWT signing secret

inside Blazor WebAssembly.

## 4. Recommended request flow

```text
Blazor WebAssembly
        ↓
ASP.NET Core Web API
        ↓
Application / Domain
        ↓
EF Core + Npgsql
        ↓
Supabase PostgreSQL
```

By default the frontend does not query PostgreSQL directly.

## 5. Backend structure

```text
apps/api/
├── Hrm.sln
├── src/
│   ├── Hrm.Api/
│   ├── Hrm.SharedKernel/
│   └── Modules/
│       ├── Auth/
│       ├── Employees/
│       ├── Attendance/
│       ├── Leave/
│       ├── Payroll/
│       ├── Products/
│       ├── Suppliers/
│       ├── Warehouses/
│       ├── Inventory/
│       ├── Procurement/
│       ├── Sales/
│       ├── Reports/
│       └── Audit/
└── tests/
```

Each backend module:

```text
Module/
├── Domain/
├── Application/
├── Infrastructure/
└── Presentation/
```

## 6. Frontend structure

```text
apps/web/
├── Hrm.Web.csproj
├── Program.cs
├── App.razor
├── Routes.razor
├── Layout/
├── Pages/
├── Features/
├── Services/
├── Authorization/
└── Shared/
```

## 7. Persistence rules
- PostgreSQL hosted on Supabase.
- EF Core + Npgsql.
- Schema changes use EF Core Migrations.
- Important history data must not be hard-deleted.
- Indexes are reviewed for search/filter/report workloads.

## 8. Security
- Backend enforces RBAC using ASP.NET Core policies/handlers.
- Frontend authorization is for UX only.
- Secrets are never hardcoded.
- Supabase service-role key must never be placed in Blazor WebAssembly.
