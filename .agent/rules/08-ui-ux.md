# UI/UX Rules
- Admin, Manager, Employee có navigation khác nhau theo quyền.
- Admin dùng route `/admin/*`.
- Form phải hiển thị validation rõ.
- Bảng dữ liệu hỗ trợ search/filter khi PRD yêu cầu.
- Hành động phá hủy/khóa/finalize cần confirmation.

## Multi-domain UI Separation
- `/admin/*`: Admin
- `/hr/*`: HR Manager
- `/warehouse/*`: Warehouse
- `/procurement/*`: Procurement
- `/sales/*`: Sales
- Employee self-service dùng shell riêng phù hợp.

Có thể dùng chung design system nhưng không dùng chung navigation/menu nghiệp vụ.
