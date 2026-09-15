# Database Rules
- PostgreSQL + Prisma.
- UUID cho khóa chính.
- Core table có `createdAt`, `updatedAt`.
- Dữ liệu lịch sử dùng status/soft delete thay hard delete.
- Schema change phải có migration.
- Payroll, attendance, leave phải giữ lịch sử.
