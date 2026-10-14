namespace Hrm.Modules.Auth.Infrastructure.Persistence;

public interface IAuthDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class AuthDatabaseInitializer : IAuthDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Tắt tự động khởi tạo/seed để tránh xung đột dữ liệu dùng chung trên Supabase.
        // Dữ liệu và schema được quản trị thủ công qua scripts/supabase_schema_and_seed.sql.
        return Task.CompletedTask;
    }
}
