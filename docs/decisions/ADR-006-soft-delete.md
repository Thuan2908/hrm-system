# ADR-006-soft-delete: Preserve Historical Data

Status: Accepted

## Context
HR/payroll cần lịch sử.

## Decision
Dùng status/soft delete thay hard delete cho entity quan trọng.

## Consequences
- Tăng tính nhất quán và khả năng audit.
- Mọi thay đổi trái quyết định này phải có ADR thay thế.
