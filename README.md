# HRM System — Saigon Retail JSC

Bộ tài liệu này là khung triển khai dự án HRM theo hướng **AI-agent friendly / vibe coding có kiểm soát**.

## Nguồn yêu cầu
Bộ tài liệu được xây dựng dựa trên PRD/Báo cáo đặc tả của Saigon Retail JSC:
- 12 chi nhánh tại TP.HCM
- khoảng 250 nhân sự
- 4 nhóm vấn đề chính: hồ sơ phân tán, chấm công/đơn từ thủ công, tính lương sai lệch/chậm, thiếu RBAC/backup/BI
- 5 phân hệ chính: Admin, Hồ sơ nhân sự, Chấm công & Đơn từ, Tiền lương, Báo cáo BI
- 4 tác nhân chính: Admin, Người quản lý, Nhân viên, Cổng ngân hàng

## Cách dùng
Agent phải đọc theo thứ tự:
1. `AGENTS.md`
2. `docs/product/vision.md`
3. `docs/product/scope.md`
4. `docs/product/business-rules.md`
5. `ARCHITECTURE.md`
6. `docs/architecture/module-boundaries.md`
7. `docs/decisions/*`
8. `.agent/rules/*`
9. `task/README.md`
10. task cụ thể đang được giao

## Nguyên tắc
- Tài liệu product/business bám sát PRD.
- Các quyết định kỹ thuật như Next.js, NestJS, PostgreSQL, Prisma, monorepo, modular monolith là lựa chọn kiến trúc bổ sung để triển khai.
- Nếu requirement mới mâu thuẫn PRD, phải ghi rõ trong ADR hoặc Change Request trước khi code.
# hrm-system
