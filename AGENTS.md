# AGENTS.md

## 1. Mục tiêu
Hướng dẫn mọi AI agent và developer làm việc thống nhất trên dự án HRM.

## 2. Trình tự bắt buộc trước khi code
1. Đọc `docs/product/business-rules.md`.
2. Đọc `ARCHITECTURE.md`.
3. Đọc ADR liên quan.
4. Đọc rule của tầng FE/BE/DB/Security.
5. Đọc task hiện tại.
6. Kiểm tra dependency và code hiện có trước khi tạo mới.

## 3. Quy tắc phạm vi
- Không tự thêm nghiệp vụ ngoài PRD nếu chưa có Change Request.
- Không refactor file không liên quan.
- Không đổi schema lớn khi chưa cập nhật ADR.
- Không thay đổi naming convention riêng lẻ theo ý agent.
- Không hard-delete dữ liệu lịch sử nhân sự, chấm công, đơn từ, bảng lương.

## 4. Quy tắc kiến trúc
- Monorepo.
- Modular Monolith.
- FE: Next.js + TypeScript.
- BE: NestJS + TypeScript.
- DB: PostgreSQL + Prisma.
- API versioning `/api/v1`.
- RBAC được kiểm tra ở backend.
- User và Employee là hai khái niệm khác nhau.

## 5. Quy tắc hoàn thành task
Một task chỉ được Done khi:
- đúng Acceptance Criteria;
- có validation;
- có authorization nếu liên quan;
- có test tương ứng;
- không phá rule/ADR;
- cập nhật docs nếu thay đổi hành vi;
- cập nhật trạng thái trong task.

## 6. Khi thiếu thông tin
Agent phải:
- ghi rõ assumption;
- chọn giải pháp ít rủi ro nhất;
- không bịa business rule;
- nếu assumption ảnh hưởng dữ liệu, payroll, security hoặc permission thì phải dừng và yêu cầu quyết định.
