# Product Scope

Hệ thống được thiết kế chuyên biệt cho **HRM System (Quản trị Nhân sự)** theo kiến trúc Monorepo + Modular Monolith, tập trung số hóa toàn diện quy trình nhân sự, chấm công, nghỉ phép, tính lương và phân quyền.

## In Scope

### 1. Admin & Security
- Authentication (JWT + Refresh token rotation)
- Account management (Tạo, khóa, kích hoạt, reset mật khẩu)
- RBAC (Role-Based Access Control)
- Admin UI riêng biệt
- Advanced search/filter/sort
- Audit log ghi nhận lịch sử thao tác
- Quản trị bảo mật và phiên làm việc

### 2. HRM Core
- Hồ sơ nhân sự (Employee profile)
- Quản lý phòng ban (Department) & Chức vụ (Position)
- Chấm công & làm thêm giờ (Attendance & OT)
- Đơn từ nghỉ phép / thai sản / ốm đau / thôi việc (Leave requests & Approvals)
- Tính & chốt lương, phiếu lương (Payroll & Payslip)
- Báo cáo nhân sự & thống kê (HR Reports)
- Cổng thông tin nhân viên tự phục vụ (Employee Self-Service)
- Lưu trữ tài liệu hồ sơ (Files)

## Out of Scope Baseline
- AI attrition prediction
- FaceID mobile attendance
- Full CRM / ERP
- Kế toán tổng hợp (General Ledger)
- Chuỗi cung ứng, kho bãi và bán lẻ (Warehouse, Inventory, Sales, Procurement)

Các chức năng ngoài scope chỉ được thêm qua Change Request/ADR.
