# AGENTS.md

## Stack bắt buộc
- Architecture: Monorepo + Modular Monolith
- Frontend: .NET Blazor WebAssembly + C#
- Backend: ASP.NET Core Web API + C#
- ORM: Entity Framework Core + Npgsql
- Database: PostgreSQL hosted on Supabase

## Supabase rules
- `SUPABASE_URL` và `SUPABASE_ANON_KEY` là client/API configuration.
- Không dùng anon key làm PostgreSQL credential.
- Backend DB connection string dùng secret/env riêng.
- Không đưa DB password hoặc service-role key vào frontend.

## Trước khi code
1. Đọc business rules.
2. Đọc `ARCHITECTURE.md`.
3. Đọc ADR liên quan.
4. Đọc FE/BE/DB/Security rules.
5. Đọc task.
6. Kiểm tra code hiện có.

## Frontend
- Razor component không chứa domain business logic.
- API call qua typed service/client.
- Layout tách Admin/HR/Employee.
- Không truy cập database trực tiếp từ Blazor.

## Backend
- Controller không chứa business logic.
- Dùng DI.
- Dùng async/await.
- Dùng CancellationToken khi phù hợp.
- Persistence qua EF Core + Npgsql.
- Cross-module access qua public contract.

## Secrets
Không commit:
- DB password
- Supabase service-role key
- JWT signing key
- production connection string
