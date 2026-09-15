# Roles & Permissions

## Roles baseline
### ADMIN
- quản lý account
- reset password
- khóa/mở tài khoản
- gán role/permission
- xem audit log
- cấu hình backup/restore

### HR_MANAGER
- quản lý hồ sơ nhân sự
- điều chuyển/bổ nhiệm
- offboarding
- duyệt đơn cấp HR
- chốt bảng công
- tính/chốt payroll
- xem báo cáo

### LEADER
- xem nhân sự thuộc phạm vi phụ trách
- duyệt bước 1 đơn từ
- duyệt bảng công nhóm nếu được cấp quyền

### EMPLOYEE
- xem/sửa thông tin cá nhân được phép
- check-in/out
- gửi đơn
- xem trạng thái đơn
- xem/in payslip cá nhân

## Rule
FE có thể ẩn/hiện menu, nhưng backend phải kiểm tra permission cho mọi API.
