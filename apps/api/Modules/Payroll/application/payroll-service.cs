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
    Task<YearlyPayrollSummaryDto> GetMyYearlyPayslipsAsync(long userId, short year, CancellationToken cancellationToken);
    Task<IReadOnlyList<PayrollManagementItemDto>> GetAllPayslipsAsync(short? month, short? year, CancellationToken cancellationToken);
    Task<int> CalculatePeriodPayrollAsync(short month, short year, long actorUserId, CancellationToken cancellationToken);
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

    public async Task<YearlyPayrollSummaryDto> GetMyYearlyPayslipsAsync(long userId, short year, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);

        var list = await dbContext.Payrolls.AsNoTracking()
            .Where(p => p.EmployeeId == employeeId && p.YearPeriod == year)
            .OrderBy(p => p.MonthPeriod)
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

        var totalDays = list.Sum(p => p.ActualDays);
        var totalGross = list.Sum(p => p.GrossSalary);
        var totalBhxh = list.Sum(p => p.BhxhDeduct);
        var totalTax = list.Sum(p => p.TaxDeduct);
        var totalNet = list.Sum(p => p.NetSalary);

        return new YearlyPayrollSummaryDto(
            YearPeriod: year,
            TotalActualDays: totalDays,
            TotalGrossSalary: totalGross,
            TotalBhxhDeduct: totalBhxh,
            TotalTaxDeduct: totalTax,
            TotalNetSalary: totalNet,
            MonthlyRecords: list
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

    public async Task<IReadOnlyList<PayrollManagementItemDto>> GetAllPayslipsAsync(short? month, short? year, CancellationToken cancellationToken)
    {
        var query = dbContext.Payrolls.AsNoTracking().AsQueryable();
        if (month.HasValue) query = query.Where(p => p.MonthPeriod == month.Value);
        if (year.HasValue) query = query.Where(p => p.YearPeriod == year.Value);

        var payrolls = await query
            .OrderByDescending(p => p.YearPeriod)
            .ThenByDescending(p => p.MonthPeriod)
            .ToListAsync(cancellationToken);

        var employeeIds = payrolls.Select(p => p.EmployeeId).Distinct().ToList();
        var employees = await authDbContext.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        return payrolls.Select(p =>
        {
            employees.TryGetValue(p.EmployeeId, out var emp);
            return new PayrollManagementItemDto(
                p.Id,
                p.EmployeeId,
                emp?.Code ?? "--",
                emp?.FullName ?? "Nhân viên #" + p.EmployeeId,
                emp?.Department?.Name ?? "N/A",
                p.MonthPeriod,
                p.YearPeriod,
                p.ActualDays,
                p.GrossSalary,
                p.BhxhDeduct,
                p.TaxDeduct,
                p.NetSalary
            );
        }).ToList();
    }

    public async Task<int> CalculatePeriodPayrollAsync(short month, short year, long actorUserId, CancellationToken cancellationToken)
    {
        var employees = await authDbContext.Employees.AsNoTracking()
            .Include(e => e.Position)
            .Where(e => e.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        if (employees.Count == 0) return 0;

        var maxId = await dbContext.Payrolls.MaxAsync(p => (long?)p.Id, cancellationToken) ?? 0;
        var processedCount = 0;

        foreach (var emp in employees)
        {
            var existing = await dbContext.Payrolls
                .FirstOrDefaultAsync(p => p.EmployeeId == emp.Id && p.MonthPeriod == month && p.YearPeriod == year, cancellationToken);

            const decimal standardDays = 22m;
            // Lấy lương cơ bản của nhân viên (mặc định 10 triệu nếu chưa set)
            var baseSalary = emp.BaseSalary > 0 ? emp.BaseSalary : 10_000_000m;
            // Tính phụ cấp chức vụ từ allowance_rate trong bảng positions
            var allowanceRate = emp.Position?.AllowanceRate ?? 0m;
            var positionAllowance = Math.Round(baseSalary * allowanceRate, 0);

            var gross = baseSalary + positionAllowance;
            var bhxh = Math.Round(gross * 0.105m, 0); // 10.5% BHXH, BHYT, BHTN
            var taxable = Math.Max(0m, gross - 11_000_000m - bhxh); // Giảm trừ gia cảnh 11tr
            var tax = Math.Round(taxable * 0.05m, 0);
            var net = gross - bhxh - tax;

            if (existing is not null)
            {
                existing.ActualDays = standardDays;
                existing.GrossSalary = gross;
                existing.BhxhDeduct = bhxh;
                existing.TaxDeduct = tax;
                existing.NetSalary = net;
            }
            else
            {
                maxId++;
                dbContext.Payrolls.Add(new PayrollRecord
                {
                    Id = maxId,
                    EmployeeId = emp.Id,
                    MonthPeriod = month,
                    YearPeriod = year,
                    ActualDays = standardDays,
                    GrossSalary = gross,
                    BhxhDeduct = bhxh,
                    TaxDeduct = tax,
                    NetSalary = net
                });
            }
            processedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return processedCount;
    }
}
