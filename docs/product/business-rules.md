# Business Rules

## Employee
- BR-EMP-001: `emp_code` là duy nhất.
- BR-EMP-002: Nhân viên có một department chính và một position chính tại một thời điểm.
- BR-EMP-003: Onboarding tạo hồ sơ nhân viên và khởi tạo tài khoản khi đủ điều kiện.
- BR-EMP-004: Điều chuyển/bổ nhiệm phải cập nhật department/position và quyền liên quan.
- BR-EMP-005: Offboarding chuyển trạng thái sang `RESIGNED`, khóa tài khoản, không xóa cứng hồ sơ.

## Attendance
- BR-ATT-001: Attendance gắn với employee và work_date.
- BR-ATT-002: Hệ thống lưu check-in/check-out và tính thời gian làm thực tế/OT.
- BR-ATT-003: Dữ liệu đã dùng để chốt payroll không được sửa tùy ý; thay đổi cần audit.

## Leave
- BR-LEAVE-001: Employee nộp đơn online.
- BR-LEAVE-002: Luồng duyệt baseline 2 cấp: Leader -> HR Manager.
- BR-LEAVE-003: Người dùng không được tự duyệt đơn của chính mình.
- BR-LEAVE-004: Approve/reject phải lưu người duyệt, thời điểm và ghi chú.
- BR-LEAVE-005: Các loại baseline: phép năm, ốm, thai sản, thôi việc.

## Payroll
- BR-PAY-001: Payroll theo tháng/năm cho từng employee.
- BR-PAY-002: Công thức baseline:
  `Net Salary = (Base Salary / 26) * Actual Days + Allowance + OT Pay - BHXH - PIT - Advance`
- BR-PAY-003: Tỷ lệ BHXH baseline trong PRD là 10.5%.
- BR-PAY-004: Payroll phải thể hiện actual days, OT, gross, BHXH, tax, net.
- BR-PAY-005: Payroll finalized không được chỉnh trực tiếp; điều chỉnh phải có audit.
- BR-PAY-006: Nhân viên chỉ xem payslip của chính mình trừ khi có permission đặc biệt.

## RBAC
- BR-RBAC-001: Quyền theo role/permission.
- BR-RBAC-002: Khi thăng chức, quyền liên quan phải được cập nhật theo quyết định mới.
- BR-RBAC-003: Backend là authority cuối cùng.

## Reporting
- BR-RPT-001: Có báo cáo biến động nhân sự theo tháng.
- BR-RPT-002: Có thống kê theo trình độ, thâm niên, dải lương.
- BR-RPT-003: Dashboard chỉ hiển thị dữ liệu người dùng có quyền xem.

## Backup
- BR-BKP-001: Có full backup định kỳ.
- BR-BKP-002: Có differential/incremental phù hợp.
- BR-BKP-003: Có transaction-log/point-in-time strategy tương đương cho dữ liệu quan trọng.
- BR-BKP-004: Restore phải được thử nghiệm định kỳ.

## Admin Account Management
- BR-ADM-001: Admin UI phải tách biệt với HR, Warehouse, Sales và Employee UI.
- BR-ADM-002: Admin được tìm kiếm account theo keyword: full name hoặc username.
- BR-ADM-003: Admin được lọc theo Department, Role, Account Status.
- BR-ADM-004: Admin được sort theo CreatedAt và LastLogin.
- BR-ADM-005: Account không hard-delete; thao tác "xóa" nghiệp vụ chuyển thành Deactivate/Disable.
- BR-ADM-006: Mọi thay đổi role/permission/account status phải có audit.

## Product
- BR-PRD-001: ProductCode là duy nhất.
- BR-PRD-002: Product có trạng thái Active/Inactive.
- BR-PRD-003: Product có thể được tìm kiếm theo code/name.
- BR-PRD-004: Product hỗ trợ filter theo category/status/supplier baseline.
- BR-PRD-005: Product hỗ trợ sort theo name/createdAt/updatedAt.

## Supplier
- BR-SUP-001: SupplierCode là duy nhất.
- BR-SUP-002: Supplier có trạng thái Active/Inactive.
- BR-SUP-003: Supplier hỗ trợ search theo code/name/contact.
- BR-SUP-004: Supplier hỗ trợ filter theo status và sort theo name/createdAt.
- BR-SUP-005: Supplier đang được tham chiếu bởi PO/history không được hard-delete.

## Warehouse & Inventory
- BR-WHS-001: WarehouseCode là duy nhất.
- BR-WHS-002: Inventory được xác định tối thiểu bởi Warehouse + Product.
- BR-WHS-003: Stock quantity không được âm nếu không có business exception được phê duyệt.
- BR-WHS-004: Mọi nhập/xuất/chuyển kho phải tạo StockMovement.
- BR-WHS-005: StockMovement đã posted không được sửa trực tiếp; phải reverse/adjust có audit.
- BR-WHS-006: Inventory hỗ trợ search/filter/sort theo warehouse/product/category/quantity/status.

## Procurement
- BR-PUR-001: Purchase Order gắn với Supplier.
- BR-PUR-002: Goods Receipt cập nhật tồn kho qua StockMovement.
- BR-PUR-003: PO/Receipt sau khi finalized không hard-delete.

## Sales
- BR-SAL-001: Sales Order có ít nhất một item.
- BR-SAL-002: Sales Order item tham chiếu Product hợp lệ.
- BR-SAL-003: Khi đơn bán được xác nhận xuất kho, inventory giảm thông qua StockMovement.
- BR-SAL-004: Sales screens phải tách khỏi Admin và HR screens.
