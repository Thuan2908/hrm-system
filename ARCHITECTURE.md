# ARCHITECTURE.md

## 1. Kiến trúc cấp cao
Dự án dùng **Monorepo + Modular Monolith**.

```text
hrm-system/
├── apps/
│   ├── web        # Next.js
│   └── api        # NestJS
├── packages/
│   ├── ui
│   ├── shared-types
│   ├── validation
│   └── configs
├── docs/
├── .agent/
└── task/
```

## 2. Boundary chính
- Auth / Users / RBAC
- Employees
- Departments
- Positions
- Attendance
- Leave
- Payroll
- Reports
- Audit
- Files
- Backup/Restore orchestration
- Bank export integration

## 3. Nguyên tắc dependency
- Controller -> Application Service -> Domain/Repository
- Module chỉ gọi public service/interface của module khác.
- Không truy cập repository của module khác trực tiếp.
- Không đặt business logic trong controller hoặc component UI.

## 4. Data ownership
- Auth sở hữu Users/Roles/Permissions.
- Employees sở hữu hồ sơ nhân sự.
- Attendance sở hữu chấm công.
- Leave sở hữu đơn từ.
- Payroll sở hữu bảng lương/payslip.
- Reports chỉ đọc qua query/service contract, không sửa domain data.

## 5. Non-functional
- Audit mọi thao tác nhạy cảm.
- Soft delete / status transition cho dữ liệu cần lịch sử.
- Backup và restore có quy trình rõ ràng.
- Admin route tách biệt với employee/manager UI.
