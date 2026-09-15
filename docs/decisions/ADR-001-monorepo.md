# ADR-001-monorepo: Use Monorepo

Status: Accepted

## Context
FE, BE và shared package cần version đồng bộ.

## Decision
Dùng pnpm workspace + Turborepo.

## Consequences
- Tăng tính nhất quán và khả năng audit.
- Mọi thay đổi trái quyết định này phải có ADR thay thế.
