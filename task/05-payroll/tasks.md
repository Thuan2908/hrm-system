# Epic 05 — Payroll

## PAY-001 — Payroll period
**Mục tiêu:** Tạo kỳ lương.

**Acceptance Criteria:**
- Unique month/year
- Có trạng thái
- Không trùng kỳ

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-002 — Load closed attendance
**Mục tiêu:** Nạp dữ liệu công đã chốt.

**Acceptance Criteria:**
- Chỉ lấy timesheet closed
- Map actual days/OT
- Có validation thiếu dữ liệu

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-003 — Salary profile
**Mục tiêu:** Nạp lương cơ bản/phụ cấp.

**Acceptance Criteria:**
- Nguồn dữ liệu xác định
- Snapshot theo kỳ
- Không đọc giá trị tương lai

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-004 — BHXH deduction
**Mục tiêu:** Tính BHXH baseline 10.5%.

**Acceptance Criteria:**
- Không hard-code ngoài config/rule
- Có unit test
- Hiển thị trên payslip

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-005 — PIT deduction
**Mục tiêu:** Tính thuế TNCN theo rule cấu hình.

**Acceptance Criteria:**
- Tách calculation service
- Có test
- Không trộn UI logic

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-006 — Net salary
**Mục tiêu:** Tính lương thực nhận.

**Acceptance Criteria:**
- Theo công thức baseline
- Kết quả trace được từng thành phần
- Round rule thống nhất

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-007 — Payroll review
**Mục tiêu:** HR review kết quả.

**Acceptance Criteria:**
- Có anomaly flag
- Cho phép adjustment trước finalize
- Adjustment có lý do

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-008 — Finalize payroll
**Mục tiêu:** Khóa bảng lương.

**Acceptance Criteria:**
- Không sửa trực tiếp sau finalize
- Audit
- Chỉ HR Manager có quyền

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-009 — Payslip
**Mục tiêu:** Phát hành phiếu lương.

**Acceptance Criteria:**
- Employee xem chi tiết
- Export/print A4
- Không xem payslip người khác

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## PAY-010 — Bank export
**Mục tiêu:** Xuất file lệnh chi.

**Acceptance Criteria:**
- Chỉ payroll finalized
- Dữ liệu tài khoản ngân hàng được bảo vệ
- Có trạng thái export

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
