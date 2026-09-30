# CURRENT_STATE.md

## Trạng thái hiện tại

**Milestone 2 — Security & Admin đã hoàn thành phần implementation ngày 2026-09-16.** Repository là .NET 10 monorepo kết nối PostgreSQL trên Supabase và có frontend Blazor cho các luồng quản trị.

## Đã hoàn thành

- Solution `Hrm.slnx` gồm 18 projects: API host, Blazor WebAssembly, shared libraries, 8 business modules HRM (Auth, Employees, Attendance, Leave, Payroll, Reports, Files, Audit) và 5 test projects.
- Dọn dẹp hoàn toàn các module không liên quan (Products, Suppliers, Warehouses, Inventory, Procurement, Sales) và các permission thừa.
- Triển khai và tích hợp đầy đủ module `Files` (lưu trữ tệp, abstraction theo ADR-008, upload, download, metadata) và `Reports` (báo cáo biến động nhân sự, thâm niên, phân bố phòng ban, dải lương, chấm công).
- Central Package Management, shared build/analyzer rules, local `dotnet-ef` tool manifest và `.gitignore`.
- API v1 baseline: response envelope, exception handler, OpenAPI, CORS, liveness/readiness health checks và OpenTelemetry.
- PostgreSQL/Supabase persistence baseline bằng EF Core + Npgsql + snake_case.
- Auth persistence tương thích schema Supabase hiện có (`bigint`, BCrypt, một role/user), JWT access token, refresh-token rotation, logout và session restoration.
- Startup Development bổ sung idempotent các bảng hỗ trợ `refresh_tokens`, `user_security_states`, `user_account_metadata`, `audit_logs`, `file_attachments`; không chạy migration Identity lên schema legacy.
- RBAC backend default-deny, permission guard, role-permission administration và ADMIN override có kiểm soát.
- Blazor Admin & Employee Portal: login/logout, dashboard báo cáo trực quan, tra cứu nhân sự, chấm công, nghỉ phép, phiếu lương, phân quyền vai trò và audit log.
- Unit, architecture, API integration và Blazor component tests đã chạy thành công (21 passed, 1 skipped).

## Chưa triển khai

- PostgreSQL Testcontainers integration tests local Docker (hiện dùng PostgreSQL Supabase).
- CI/CD, deployment, backup/restore drill, monitoring production và product handover.
- Flow quên mật khẩu không thuộc phạm vi theo quyết định sản phẩm; Admin vẫn có chức năng reset mật khẩu.

## Quality gate gần nhất

- Backend build: pass, 0 warnings, 0 errors.
- Blazor build: pass, 0 warnings, 0 errors.
- Unit tests: 15 passed.
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
