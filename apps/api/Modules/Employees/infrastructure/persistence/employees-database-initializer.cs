namespace Hrm.Modules.Employees.Infrastructure.Persistence;

public interface IEmployeesDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class EmployeesDatabaseInitializer : IEmployeesDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Tắt tự động khởi tạo/seed để tránh xung đột dữ liệu dùng chung trên Supabase.
        return Task.CompletedTask;
    }
}
