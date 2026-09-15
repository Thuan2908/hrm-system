# Architecture Rules
- Modular Monolith, ranh giới module rõ.
- Cấm circular dependency.
- Cấm module A dùng repository nội bộ của module B.
- Reports là read-only.
- Cross-module write phải đi qua service contract.
