# CURRENT_STATE.md

## Trạng thái hiện tại

**Milestone 1 — Foundation baseline đã hoàn thành ngày 2026-09-16.** Repository đã chuyển từ documentation-only sang .NET 10 monorepo có thể restore, build và test.

## Đã hoàn thành

- Solution `Hrm.slnx` gồm 24 projects: API host, Blazor WebAssembly, shared libraries, 14 business modules và 5 test projects.
- Central Package Management, shared build/analyzer rules, local `dotnet-ef` tool manifest và `.gitignore`.
- API v1 baseline: response envelope, exception handler, OpenAPI, CORS, liveness/readiness health checks và OpenTelemetry.
- PostgreSQL/Supabase persistence baseline bằng EF Core + Npgsql + snake_case.
- Auth schema baseline: ASP.NET Core Identity, users, roles, permissions, role grants và refresh tokens.
- Migration đầu tiên: `InitialAuth`, có seed 9 roles, permission catalog và quyền ADMIN.
- Blazor shell: typed API client, dashboard, error boundary, anonymous authentication-state baseline và layout Admin/HR/Warehouse/Procurement/Sales/Employee.
- Unit, architecture, API integration và Blazor component tests đã chạy thành công.
- E2E project đã scaffold; smoke test được skip cho đến khi local stack và Playwright browser cùng chạy.
- Tài liệu stack đã được đồng bộ sang PostgreSQL/Npgsql, pagination `page`/`pageSize`, và ADR-014 cho .NET monorepo tooling.

## Chưa triển khai

- Auth use cases/endpoints: login, refresh, logout, forgot/reset password, session revocation và JWT issuance.
- Các vertical slice nghiệp vụ trong Employees, Attendance, Leave, Payroll, Products, Suppliers, Warehouses, Inventory, Procurement, Sales, Reports, Audit và Files.
- PostgreSQL Testcontainers integration tests thực tế và Playwright E2E thực tế.
- CI/CD, deployment, backup/restore drill, monitoring production và product handover.

## Quality gate gần nhất

- Backend build: pass, 0 warnings, 0 errors.
- Blazor build: pass, 0 warnings, 0 errors.
- Unit tests: 4 passed.
- Architecture tests: 1 passed.
- API integration tests: 2 passed.
- Component tests: 1 passed.
- E2E: 1 skipped có chủ đích vì cần running stack/browser.

## Milestone tiếp theo

Milestone 2 nên triển khai Security/Admin theo vertical slice: JWT access/refresh rotation, login/logout/refresh/password reset, RBAC authorization handler, admin user search và audit trail; sau đó mới mở rộng Core HR.

## Stack đang áp dụng

- FE: Blazor WebAssembly + C# (`net10.0`)
- BE: ASP.NET Core Web API + C# (`net10.0`)
- DB: PostgreSQL hosted on Supabase
- ORM: Entity Framework Core + Npgsql
- Architecture: Monorepo + Modular Monolith
