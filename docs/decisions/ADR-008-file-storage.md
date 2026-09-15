# ADR-008-file-storage: Attachment Abstraction

Status: Accepted

## Context
Leave request có thể có file minh chứng.

## Decision
Dùng storage abstraction, không hard-code local path trong domain.

## Consequences
- Tăng tính nhất quán và khả năng audit.
- Mọi thay đổi trái quyết định này phải có ADR thay thế.
