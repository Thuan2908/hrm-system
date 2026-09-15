# ADR-009-admin-separation: Separate Admin UI

Status: Accepted

## Context
PRD yêu cầu admin route/layout tách biệt.

## Decision
Dùng `/admin/*` với layout và guard riêng.

## Consequences
- Tăng tính nhất quán và khả năng audit.
- Mọi thay đổi trái quyết định này phải có ADR thay thế.
