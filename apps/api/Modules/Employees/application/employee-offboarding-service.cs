using Hrm.Modules.Auth.Contracts;
using Hrm.Modules.Auth.Domain;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.Modules.Employees.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Employees.Application;

public interface IEmployeeOffboardingService
{
    Task OffboardAsync(long employeeId, long actorUserId, CancellationToken cancellationToken);
}

public sealed class EmployeeOffboardingService(
    EmployeesDbContext dbContext,
    AuthDbContext authDbContext,
    IEmployeeAccessRevoker accessRevoker,
    TimeProvider timeProvider) : IEmployeeOffboardingService
{
    public async Task OffboardAsync(
        long employeeId,
        long actorUserId,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var employee = await dbContext.Employees.SingleOrDefaultAsync(
                item => item.Id == employeeId,
                cancellationToken)
                ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");

            employee.Status = "RESIGNED";
            await dbContext.SaveChangesAsync(cancellationToken);

            var now = timeProvider.GetUtcNow();
            authDbContext.EmployeeTimelineEvents.Add(new EmployeeTimelineEvent
            {
                EmployeeId = employeeId,
                EventType = "OFFBOARDING",
                Title = "Thôi việc & Thu hồi quyền truy cập",
                Description = "Hoàn tất thủ tục thôi việc và thu hồi quyền truy cập tài khoản trên hệ thống.",
                Timestamp = now,
                CreatedByUserId = actorUserId
            });
            await authDbContext.SaveChangesAsync(cancellationToken);

            await accessRevoker.RevokeAsync(employeeId, actorUserId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
