# ADR-012: Do Not Hard Delete Accounts

Status: Accepted

## Context
Account có liên kết với audit, permission và lịch sử nghiệp vụ.

## Decision
Thao tác "xóa account" được hiện thực bằng:
- Lock
- Deactivate

Không hard-delete account đã từng được sử dụng.

## Consequences
- Giữ được audit/history.
- UI cần dùng wording rõ: Deactivate thay vì Delete khi phù hợp.
