namespace Hrm.Modules.Attendance.Infrastructure.Persistence;

public interface IAttendanceDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class AttendanceDatabaseInitializer : IAttendanceDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Tắt tự động khởi tạo/seed để tránh xung đột dữ liệu dùng chung trên Supabase.
        return Task.CompletedTask;
    }
}
