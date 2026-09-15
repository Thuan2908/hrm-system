# Epic 08 — Testing & Security

## QA-001 — Unit coverage critical rules
**Mục tiêu:** Test business rule chính.

**Acceptance Criteria:**
- Leave approval
- Payroll formula
- RBAC guard
- Offboarding

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## QA-002 — Integration tests
**Mục tiêu:** Test API + DB.

**Acceptance Criteria:**
- Auth
- Employee
- Attendance
- Payroll

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## QA-003 — E2E critical flows
**Mục tiêu:** Test end-to-end.

**Acceptance Criteria:**
- Onboarding -> login
- Leave -> approval
- Attendance -> payroll
- Payslip

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## QA-004 — Permission matrix test
**Mục tiêu:** Kiểm thử role/permission.

**Acceptance Criteria:**
- Employee/Leader/HR/Admin
- Denied paths
- Admin route

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## QA-005 — Security review
**Mục tiêu:** Rà soát bảo mật.

**Acceptance Criteria:**
- Input validation
- Auth token
- Access control
- Sensitive logs

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## QA-006 — Performance baseline
**Mục tiêu:** Đo API chính.

**Acceptance Criteria:**
- Employee list
- Dashboard
- Payroll batch

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## QA-007 — Backup/restore verification
**Mục tiêu:** Xác nhận dữ liệu khôi phục.

**Acceptance Criteria:**
- Checksum/logical validation
- User/account
- Payroll

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## QA-008 — UAT pack
**Mục tiêu:** Chuẩn bị kịch bản nghiệm thu.

**Acceptance Criteria:**
- Admin
- HR Manager
- Employee

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
