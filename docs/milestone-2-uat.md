# Milestone 2 — Security & Admin UAT

## Chuẩn bị

- API chạy tại `http://localhost:5172`.
- Blazor WebAssembly chạy tại `http://localhost:5296`.
- Dùng một tài khoản thuộc role `ADMIN`; không ghi mật khẩu vào tài liệu hoặc Git.
- Xác nhận `GET /health/ready` trả trạng thái `Healthy`.

## Critical flows

1. Đăng nhập sai mật khẩu: hiển thị lỗi an toàn, không lộ chi tiết hash/database.
2. Đăng nhập đúng: chuyển tới `/admin/dashboard`; refresh trình duyệt vẫn giữ session.
3. Tài khoản: tìm partial theo họ tên/username/mã nhân viên; kết hợp Department, Role và Status.
4. Sorting: kiểm tra CreatedAt, LastLogin và Username theo cả tăng/giảm dần.
5. Lifecycle: tạo account cho employee chưa có account; lock/unlock; deactivate/activate; reset mật khẩu.
6. RBAC: đổi role của user; thêm/bỏ permission của role; user không đủ quyền bị API từ chối.
7. Audit: các thay đổi account/role/permission xuất hiện với actor, action, time và entity.
8. Offboarding: chọn “Thôi việc”; employee chuyển `RESIGNED`, account inactive/locked và refresh token bị revoke.
9. Logout: refresh token bị revoke và route Admin chuyển về login.

## Kết quả mong đợi

- Không có hard delete account hoặc employee.
- Không có secret/token/password trong response, log hay giao diện.
- Mọi critical flow ở trên đạt trước khi đánh dấu UAT hoàn thành.
- “Quên mật khẩu” không thuộc phạm vi Milestone 2; chỉ Admin reset mật khẩu.
