# ADR-003: Use PostgreSQL on Supabase

Status: Accepted

## Context
The system needs a relational database with transaction consistency and reporting support.

## Decision
Use:
- PostgreSQL hosted on Supabase
- Entity Framework Core
- Npgsql provider

Backend connects using a separate PostgreSQL connection string.

`SUPABASE_URL` and `SUPABASE_ANON_KEY` are not the EF Core database connection credentials.

## Consequences
- Good fit for relational workloads.
- Supabase reduces database hosting/operations work.
- RLS is required if direct client-side Supabase access is introduced.
