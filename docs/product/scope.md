# Product Scope

Hệ thống **HRM — Saigon Retail JSC** là hệ thống Quản lý Nhân sự tập trung theo kiến trúc Monorepo + Modular Monolith, phục vụ toàn bộ nghiệp vụ nhân sự của công ty.

## In Scope

### 1. Admin & Security
- Authentication & Session (JWT + refresh token)
- Account management (User lifecycle: Create, Lock, Unlock, Deactivate, Reset password)
- RBAC (Role & Permission management)
- Admin UI riêng biệt (`/admin/*`)
- Advanced search/filter/sort cho tài khoản
- Audit log toàn hệ thống
- Backup/restore administration

### 2. Core HR
- Employee profile (Hồ sơ nhân sự, hợp đồng, thông tin liên hệ, ngân hàng)
- Department (Cơ cấu phòng ban / chi nhánh / cửa hàng)
- Position (Chức danh, vị trí công tác)
- Onboarding (Quy trình tiếp nhận nhân viên mới)
- Transfer / Promotion (Điều chuyển, bổ nhiệm)
- Offboarding (Quy trình thôi việc và khóa tài khoản)

### 3. Attendance & Leave
- Daily Attendance & Check-in/out
- Quản lý ca và Overtime (OT)
- Đơn từ online (Nghỉ phép năm, ốm, thai sản, việc riêng, thôi việc)
- Quy trình duyệt 2 cấp (Leader -> HR Manager)
- Tổng hợp và chốt bảng công kỳ lương

### 4. Payroll
- Tính lương tự động theo công thức chuẩn
- Khấu trừ bảo hiểm bắt buộc (BHXH, BHYT, BHTN) và thuế TNCN
- Tạm ứng và các khoản phụ cấp/giảm trừ
- Quản lý kỳ lương & chốt lương bất biến (Finalize)
- Xuất phiếu lương chi tiết (Payslip)
- Xuất file chi trả lương qua ngân hàng

### 5. Reporting & Employee Self-Service (ESS)
- Báo cáo biến động nhân sự, headcount, cơ cấu thâm niên/trình độ/dải lương
- Báo cáo chi phí tiền lương
- Cổng nhân viên tự phục vụ (Xem thông tin cá nhân, chấm công, nộp đơn, xem phiếu lương)

## Out of Scope Baseline
- Quản lý sản phẩm, danh mục hàng hóa (Product Catalog)
- Quản lý nhà cung cấp (Supplier Management)
- Quản lý kho, kiểm kê và tồn kho (Warehouse & Inventory / Stock Movements)
- Quản lý mua hàng (Procurement & Purchase Orders)
- Quản lý bán hàng, đơn bán hàng (Sales Orders & POS)
- AI attrition prediction
- FaceID mobile attendance
- Full CRM / Customer management
- Full accounting/general ledger
- Marketplace/e-commerce integration
- Advanced forecasting

Các chức năng ngoài scope chỉ được thêm qua Change Request/ADR.
