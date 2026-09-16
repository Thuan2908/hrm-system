using Hrm.Modules.Auth.Contracts;
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
    IEmployeeAccessRevoker accessRevoker) : IEmployeeOffboardingService
{
    public async Task OffboardAsync(
        long employeeId,
        long actorUserId,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees.SingleOrDefaultAsync(
            item => item.Id == employeeId,
            cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        employee.Status = "RESIGNED";
        await dbContext.SaveChangesAsync(cancellationToken);

        await accessRevoker.RevokeAsync(employeeId, actorUserId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
