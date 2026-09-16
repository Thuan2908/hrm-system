# Backend Rules

Framework: ASP.NET Core Web API
Language: C#
ORM: Entity Framework Core + Npgsql
Database: PostgreSQL hosted on Supabase

## Layers
- Presentation
- Application
- Domain
- Infrastructure

## Rules
- Controller không chứa business logic.
- Dùng built-in Dependency Injection.
- Dùng async/await cho I/O.
- Dùng CancellationToken khi phù hợp.
- Centralized exception handling.
- Validate input.
- Authorization bằng ASP.NET Core policies/handlers.
- Không expose secret.
- Không truy cập DbContext của module khác.
