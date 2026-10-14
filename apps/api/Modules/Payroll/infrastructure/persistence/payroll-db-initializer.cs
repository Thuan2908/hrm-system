namespace Hrm.Modules.Payroll.Infrastructure.Persistence;

public interface IPayrollDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class PayrollDatabaseInitializer : IPayrollDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Tắt tự động khởi tạo/seed để tránh xung đột dữ liệu dùng chung trên Supabase.
        return Task.CompletedTask;
    }
}
