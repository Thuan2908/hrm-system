# TASK SYSTEM

Folder này quản lý toàn bộ vòng đời dự án từ khởi tạo đến product handover.

## Quy trình chuẩn
`BACKLOG -> READY -> IN_PROGRESS -> REVIEW -> QA -> UAT -> DONE`

## Cấu trúc
- `00-governance`: cách quản lý task
- `01-foundation`: repo, env, CI, DB baseline
- `02-security-admin`: auth, RBAC, admin
- `03-core-hr`: employee/department/position
- `04-attendance-leave`: chấm công và đơn từ
- `05-payroll`: bảng công, payroll, payslip, bank export
- `06-reporting-ess`: dashboard và self-service
- `07-data-migration-operations`: migration, backup, restore
- `08-testing-security`: test/hardening
- `09-deployment`: staging/prod cutover
- `10-product-handover`: docs, training, handover, post-launch
- `templates`: mẫu task/epic/bug/change request

## Quy tắc
Mỗi task phải có:
- ID
- Mục tiêu
- Nguồn requirement
- Scope
- Acceptance Criteria
- Dependencies
- Security/Data impact
- Test plan
- Definition of Done
