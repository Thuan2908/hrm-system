# Environment Example

## Public/client settings

```text
SUPABASE_URL=https://xrieskmmmlgtonltuina.supabase.co
SUPABASE_ANON_KEY=<your anon key>
```

## Backend-only settings

```text
ConnectionStrings__DefaultConnection=<Supabase PostgreSQL connection string>
Jwt__SigningKey=<secret>
```

Never expose DB password or Supabase service-role key in Blazor WebAssembly.
