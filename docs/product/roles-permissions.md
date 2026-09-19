# Roles & Permissions

## ADMIN
- Quản lý account
- Tạo account
- Reset password
- Khóa/Mở khóa/Deactivate account
- Gán role và permission
- Xem audit log
- Quản trị backup/restore
- Tìm kiếm nâng cao account
- Lọc và sắp xếp account

## HR_MANAGER
- Quản lý hồ sơ nhân sự
- Điều chuyển/bổ nhiệm
- Offboarding
- Duyệt đơn cấp HR
- Chốt bảng công
- Tính/chốt payroll
- Xem HR reports

## LEADER
- Xem nhân sự thuộc nhóm
- Duyệt đơn bước 1
- Duyệt bảng công nhóm khi được cấp quyền

## EMPLOYEE
- Xem/sửa thông tin cá nhân được phép
- Check-in/out
- Gửi đơn
- Xem trạng thái đơn
- Xem/in payslip

## WAREHOUSE_MANAGER
- Quản lý kho
- Xem tồn kho
- Tạo/duyệt stock movement theo quyền
- Tìm kiếm/lọc/sắp xếp sản phẩm tồn kho
- Xem báo cáo tồn kho

## WAREHOUSE_STAFF
- Xem tồn kho
- Ghi nhận nhập/xuất/chuyển kho theo quyền

## PROCUREMENT_MANAGER
- Quản lý nhà cung cấp
- Tạo/duyệt purchase order baseline
- Xem lịch sử nhập hàng

## SALES_MANAGER
- Quản lý catalog sản phẩm
- Xem đơn hàng
- Xem báo cáo bán hàng
- Tìm kiếm/lọc/sắp xếp sản phẩm

## SALES_STAFF
- Xem sản phẩm
- Tạo/cập nhật đơn bán hàng trong phạm vi được cấp

## Quy tắc chung
- FE chỉ ẩn/hiện chức năng theo quyền để cải thiện UX.
- Backend mới là nguồn quyết định authorization.
- Account không bị hard-delete; dùng deactivate/lock để giữ audit/history.
