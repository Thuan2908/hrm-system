# Epic 09 — Deployment

## DEP-001 — Dockerize
**Mục tiêu:** Container hóa FE/BE.

**Acceptance Criteria:**
- Build reproducible
- Env externalized
- Healthcheck

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## DEP-002 — Staging
**Mục tiêu:** Triển khai môi trường staging.

**Acceptance Criteria:**
- Seed/test data
- HTTPS
- Monitoring

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## DEP-003 — Migration runbook
**Mục tiêu:** Quy trình chạy migration.

**Acceptance Criteria:**
- Backup trước migration
- Rollback plan
- Owner rõ

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## DEP-004 — Production readiness
**Mục tiêu:** Checklist trước go-live.

**Acceptance Criteria:**
- Security
- Backup
- Observability
- UAT sign-off

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## DEP-005 — Go-live
**Mục tiêu:** Triển khai production.

**Acceptance Criteria:**
- Có cutover plan
- Có rollback
- Có hypercare

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## DEP-006 — Post-deploy validation
**Mục tiêu:** Kiểm tra sau triển khai.

**Acceptance Criteria:**
- Login
- Core APIs
- Payroll/report smoke tests

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
