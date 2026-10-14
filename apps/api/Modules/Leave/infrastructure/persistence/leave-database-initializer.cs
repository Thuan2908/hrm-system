namespace Hrm.Modules.Leave.Infrastructure.Persistence;

public interface ILeaveDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class LeaveDatabaseInitializer : ILeaveDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Tắt tự động khởi tạo/seed để tránh xung đột dữ liệu dùng chung trên Supabase.
        return Task.CompletedTask;
    }
}
