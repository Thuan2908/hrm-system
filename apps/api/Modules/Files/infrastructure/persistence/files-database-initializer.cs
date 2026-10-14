namespace Hrm.Modules.Files.Infrastructure.Persistence;

public interface IFilesDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class FilesDatabaseInitializer : IFilesDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Tắt tự động khởi tạo/seed để tránh xung đột dữ liệu dùng chung trên Supabase.
        return Task.CompletedTask;
    }
}
