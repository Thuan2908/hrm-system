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

## Quy tắc chung
- FE chỉ ẩn/hiện chức năng theo quyền để cải thiện UX.
- Backend mới là nguồn quyết định authorization.
- Account không bị hard-delete; dùng deactivate/lock để giữ audit/history.
