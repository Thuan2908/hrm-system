using Hrm.Contracts;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.Modules.Payroll.Domain;
using Hrm.Modules.Payroll.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Payroll.Application;

public interface IPayrollService
{
    Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(long userId, CancellationToken cancellationToken);
    Task<PayslipDetailDto> GetPayslipDetailAsync(long userId, long payrollId, CancellationToken cancellationToken);
}

public sealed class PayrollService(
    PayrollDbContext dbContext,
    AuthDbContext authDbContext) : IPayrollService
{
    public async Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(long userId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);

        var list = await dbContext.Payrolls.AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.YearPeriod)
            .ThenByDescending(p => p.MonthPeriod)
            .Select(p => new PayslipSummaryDto(
                p.Id,
                p.EmployeeId,
                p.MonthPeriod,
                p.YearPeriod,
                p.ActualDays,
                p.GrossSalary,
                p.BhxhDeduct,
                p.TaxDeduct,
                p.NetSalary))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<PayslipDetailDto> GetPayslipDetailAsync(long userId, long payrollId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);

        var payroll = await dbContext.Payrolls.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == payrollId && p.EmployeeId == employeeId, cancellationToken);

        if (payroll is null)
        {
            throw new DomainException("PAYROLL_NOT_FOUND", "Không tìm thấy thông tin kỳ lương.");
        }

        return new PayslipDetailDto(
            Id: payroll.Id,
            EmployeeId: payroll.EmployeeId,
            MonthPeriod: payroll.MonthPeriod,
            YearPeriod: payroll.YearPeriod,
            ActualDays: payroll.ActualDays,
            GrossSalary: payroll.GrossSalary,
            BhxhDeduct: payroll.BhxhDeduct,
            TaxDeduct: payroll.TaxDeduct,
            NetSalary: payroll.NetSalary
        );
    }

    private async Task<long> GetEmployeeIdAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await authDbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new DomainException("AUTH_USER_NOT_FOUND", "Không tìm thấy thông tin tài khoản.");
        }

        return user.EmployeeId;
    }
}
