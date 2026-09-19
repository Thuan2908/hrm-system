# CURRENT_STATE.md

## Trạng thái hiện tại

**Milestone 2 — Security & Admin đã hoàn thành phần implementation ngày 2026-09-16.** Repository là .NET 10 monorepo kết nối PostgreSQL trên Supabase và có frontend Blazor cho các luồng quản trị.

## Đã hoàn thành

- Solution `Hrm.slnx` gồm các project: API host, Blazor WebAssembly, shared libraries, các HRM business modules và test projects.
- Central Package Management, shared build/analyzer rules, local `dotnet-ef` tool manifest và `.gitignore`.
- API v1 baseline: response envelope, exception handler, OpenAPI, CORS, liveness/readiness health checks và OpenTelemetry.
- PostgreSQL/Supabase persistence baseline bằng EF Core + Npgsql + snake_case.
- Auth persistence tương thích schema Supabase hiện có (`bigint`, BCrypt, một role/user), JWT access token, refresh-token rotation, logout và session restoration.
- Startup Development bổ sung idempotent các bảng hỗ trợ `refresh_tokens`, `user_security_states`, `user_account_metadata`, `audit_logs`; không chạy migration Identity lên schema legacy.
- RBAC backend default-deny, permission guard, role-permission administration và ADMIN override có kiểm soát.
- Blazor Admin: login/logout, dashboard, tìm kiếm, lọc department/role/status, sort createdAt/lastLogin/username, pagination, tạo/khóa/vô hiệu hóa/reset mật khẩu/đổi role, role permissions và audit log.
- Offboarding chuyển employee sang `RESIGNED`, khóa/vô hiệu hóa account, thu hồi refresh token và ghi audit; chặn tự offboard.
- Unit, architecture, API integration và Blazor component tests đã chạy thành công.
- E2E project đã scaffold; smoke test được skip cho đến khi local stack và Playwright browser cùng chạy.
- Tài liệu stack đã được đồng bộ sang PostgreSQL/Npgsql, pagination `page`/`pageSize`, và ADR-014 cho .NET monorepo tooling.

## Chưa triển khai

- Các vertical slice nghiệp vụ trong Employees, Attendance, Leave, Payroll, Reports, Audit và Files.
- PostgreSQL Testcontainers integration tests thực tế và Playwright E2E thực tế.
- CI/CD, deployment, backup/restore drill, monitoring production và product handover.
- Flow quên mật khẩu không thuộc phạm vi theo quyết định sản phẩm; Admin vẫn có chức năng reset mật khẩu.

## Quality gate gần nhất

- Backend build: pass, 0 warnings, 0 errors.
- Blazor build: pass, 0 warnings, 0 errors.
- Unit tests: 4 passed.
- Architecture tests: 1 passed.
- API integration tests: 4 passed.
- Component tests: 1 passed.
- E2E: 1 skipped có chủ đích vì cần running stack/browser.

## Milestone tiếp theo

Milestone 3 mở rộng Core HR. Trước khi production cần chạy UAT bằng tài khoản Admin thật và bật Playwright E2E với local API/frontend đang chạy.

## Stack đang áp dụng

- FE: Blazor WebAssembly + C# (`net10.0`)
- BE: ASP.NET Core Web API + C# (`net10.0`)
- DB: PostgreSQL hosted on Supabase
- ORM: Entity Framework Core + Npgsql
- Architecture: Monorepo + Modular Monolith
