# Epic 04 — Attendance & Leave

## ATT-001 — Check-in
**Mục tiêu:** Ghi nhận giờ vào.

**Acceptance Criteria:**
- Employee active mới được chấm
- Không duplicate bất hợp lý
- Lưu timestamp

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ATT-002 — Check-out
**Mục tiêu:** Ghi nhận giờ ra.

**Acceptance Criteria:**
- Ghép đúng bản ghi trong ngày/ca
- Tính actual hours
- Validation giờ

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ATT-003 — OT calculation
**Mục tiêu:** Tính giờ OT.

**Acceptance Criteria:**
- Có rule rõ
- Không âm
- Có test case biên

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ATT-004 — Attendance list
**Mục tiêu:** Tra cứu công cá nhân/nhóm.

**Acceptance Criteria:**
- Employee chỉ xem của mình
- Leader xem phạm vi nhóm
- Filter period

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ATT-005 — Timesheet review
**Mục tiêu:** HR rà soát bảng công.

**Acceptance Criteria:**
- Có trạng thái draft/reviewed
- Chỉnh sửa có audit
- Hiển thị anomaly

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ATT-006 — Timesheet close
**Mục tiêu:** Chốt bảng công kỳ.

**Acceptance Criteria:**
- Không sửa trực tiếp sau chốt
- Payroll chỉ đọc dữ liệu đã chốt
- Có audit

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## ATT-007 — Attendance dispute
**Mục tiêu:** Ghi nhận khiếu nại công.

**Acceptance Criteria:**
- Employee tạo yêu cầu
- HR xử lý có note
- Lưu history

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-001 — Create leave request
**Mục tiêu:** Nộp đơn online.

**Acceptance Criteria:**
- Loại đơn baseline hỗ trợ
- Ngày hợp lệ
- Có reason/attachment nếu cần

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-002 — Leader approval
**Mục tiêu:** Duyệt cấp 1.

**Acceptance Criteria:**
- Chỉ leader phù hợp
- Không tự duyệt
- Lưu note/time

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-003 — HR approval
**Mục tiêu:** Duyệt cấp 2.

**Acceptance Criteria:**
- Chỉ HR Manager
- Chỉ xử lý sau cấp 1
- Lưu final decision

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-004 — Reject request
**Mục tiêu:** Từ chối đơn.

**Acceptance Criteria:**
- Bắt buộc reason
- Status nhất quán
- Employee nhận được kết quả

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-005 — Leave list
**Mục tiêu:** Tra cứu đơn.

**Acceptance Criteria:**
- Filter status/type/date
- Employee chỉ xem của mình
- Approver xem queue

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-006 — Attachment
**Mục tiêu:** File minh chứng.

**Acceptance Criteria:**
- Metadata lưu an toàn
- Permission download
- Không public URL vô thời hạn

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-007 — Leave -> attendance sync
**Mục tiêu:** Đơn duyệt ảnh hưởng công.

**Acceptance Criteria:**
- Approved leave map đúng ngày
- Không tạo double-count
- Có test integration

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.

## LEV-008 — Resignation request
**Mục tiêu:** Đơn xin thôi việc.

**Acceptance Criteria:**
- Có workflow duyệt
- Sau quyết định gọi offboarding flow
- Không hard delete dữ liệu

**Definition of Done:** theo `task/00-governance/definition-of-done.md`.
