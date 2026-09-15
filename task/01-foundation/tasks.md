# Epic 01 — Foundation

## FND-001 — Khởi tạo monorepo
**Mục tiêu:** Tạo workspace FE/BE/shared packages.

**Acceptance Criteria:**
- Có apps/web, apps/api, packages/*
- Có lint/format/typecheck scripts
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
**Mục tiêu:** Khởi tạo PostgreSQL + Prisma schema cơ sở.

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
- Lint pass
- Typecheck pass
- Test pass trước merge

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
