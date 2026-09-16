# Epic 07 — Data Migration & Operations

## OPS-001 — Excel mapping
**Mục tiêu:** Lập mapping dữ liệu cũ.

**Acceptance Criteria:**
- Field map rõ
- Xử lý duplicate
- Có data quality report

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## OPS-002 — Import employees
**Mục tiêu:** Nhập hồ sơ nhân sự.

**Acceptance Criteria:**
- Dry-run
- Validation
- Rollback batch

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## OPS-003 — Parallel cutover
**Mục tiêu:** Chạy song song 1 tháng.

**Acceptance Criteria:**
- Đối soát hàng tuần
- Ghi discrepancy
- Có tiêu chí kết thúc cutover

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## OPS-004 — Full backup
**Mục tiêu:** Thiết lập full backup.

**Acceptance Criteria:**
- Lịch định kỳ
- Encrypted storage nếu áp dụng
- Log job

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## OPS-005 — Differential backup
**Mục tiêu:** Thiết lập backup giữa kỳ.

**Acceptance Criteria:**
- Schedule
- Retention
- Monitoring

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## OPS-006 — Point-in-time strategy
**Mục tiêu:** Giảm mất dữ liệu giao dịch.

**Acceptance Criteria:**
- Thiết lập PostgreSQL point-in-time recovery/restore phù hợp với Supabase plan
- Xác định retention và giới hạn của Supabase backup/PITR
- Test restore
- RPO documented

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## OPS-007 — Restore drill
**Mục tiêu:** Diễn tập khôi phục.

**Acceptance Criteria:**
- Runbook
- RTO measured
- Evidence lưu lại

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## OPS-008 — Monitoring
**Mục tiêu:** Theo dõi hệ thống.

**Acceptance Criteria:**
- API health
- DB health
- Backup job alert

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
