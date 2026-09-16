# Epic 01 — Foundation

## FND-001 — Khởi tạo monorepo
**Mục tiêu:** Tạo workspace FE/BE/shared packages.

**Acceptance Criteria:**
- Có apps/web, apps/api, shared/* và tests/*
- Có .NET solution, Central Package Management và shared build properties
- README chạy local rõ ràng

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## FND-002 — Environment baseline
**Mục tiêu:** Chuẩn hóa env local/dev/test.

**Acceptance Criteria:**
- Có env contract
- Secret không commit
- Có config validation

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## FND-003 — Database baseline
**Mục tiêu:** Khởi tạo PostgreSQL (Supabase) + Entity Framework Core (Npgsql) schema cơ sở.

**Acceptance Criteria:**
- Có migration đầu tiên
- Có seed role/permission baseline
- Có naming convention

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## FND-004 — API baseline
**Mục tiêu:** Tạo API v1, error format, validation pipe.

**Acceptance Criteria:**
- Health endpoint hoạt động
- Error wrapper thống nhất
- Validation DTO hoạt động

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## FND-005 — Frontend baseline
**Mục tiêu:** Tạo shell, auth layout, admin layout.

**Acceptance Criteria:**
- Route groups hoạt động
- API client chuẩn
- Error boundary/loading baseline

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## FND-006 — CI baseline
**Mục tiêu:** Thiết lập pipeline quality gate.

**Acceptance Criteria:**
- `dotnet format --verify-no-changes` pass
- `dotnet build -c Release` pass
- `dotnet test -c Release` pass trước merge

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## FND-007 — Configure Supabase PostgreSQL
**Mục tiêu:** Kết nối ASP.NET Core backend với PostgreSQL trên Supabase.

**Acceptance Criteria:**
- EF Core dùng Npgsql.
- DB connection string lấy từ secret/env.
- Anon key không được dùng làm DB credential.
- EF Core migration chạy được.
- Database health check hoạt động.

## FND-008 — Blazor Frontend Baseline
**Mục tiêu:** Khởi tạo Blazor WebAssembly frontend.

**Acceptance Criteria:**
- Có `App.razor` và routing.
- Có layout riêng cho từng business area.
- Có typed API client.
- Có authorization state baseline.
