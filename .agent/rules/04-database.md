# Database Rules

- Database Engine: PostgreSQL hosted on Supabase.
- ORM: Entity Framework Core.
- Provider: Npgsql.
- Primary key: ưu tiên Guid/uuid.
- Schema change dùng EF Core Migrations.
- Core table có CreatedAt/UpdatedAt.
- Dữ liệu lịch sử dùng status/soft delete.
- Payroll, attendance, leave, audit giữ lịch sử.
- Cân nhắc index cho search/filter/report.
- Không dùng PostgreSQL-specific syntax.
- Không dùng Supabase anon key để EF Core kết nối database.
