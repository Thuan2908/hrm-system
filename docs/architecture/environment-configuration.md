# Environment Configuration

## Supabase client settings

```text
SUPABASE_URL=https://xrieskmmmlgtonltuina.supabase.co
SUPABASE_ANON_KEY=<provided anon key>
```

These are not the PostgreSQL database credentials.

## Backend database settings

```text
ConnectionStrings__DefaultConnection=<Supabase PostgreSQL connection string>
```

Backend secrets should be provided via:
- `dotnet user-secrets` for local development, or
- environment variables / secret manager for deployment.

## Important
Do not put these in Blazor WebAssembly:
- database password
- service-role key
- JWT signing secret

If the frontend calls Supabase directly in the future, Row Level Security must be designed and enabled appropriately.
