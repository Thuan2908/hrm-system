using Hrm.Contracts;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.Modules.Payroll.Domain;
using Hrm.Modules.Payroll.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Payroll.Application;

public interface IPayrollService
{
    Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(long userId, short? year, long? targetEmployeeId, CancellationToken cancellationToken);
    Task<PayslipDetailDto> GetPayslipDetailAsync(long userId, long payrollId, CancellationToken cancellationToken);
    Task<PayslipDetailDto> GetPayslipForPeriodAsync(long userId, short month, short year, long? targetEmployeeId, CancellationToken cancellationToken);
    Task<YearlyPayrollSummaryDto> GetMyYearlyPayslipsAsync(long userId, short year, CancellationToken cancellationToken);
    Task<IReadOnlyList<PayrollManagementItemDto>> GetAllPayslipsAsync(short? month, short? year, CancellationToken cancellationToken);
    Task<int> CalculatePeriodPayrollAsync(short month, short year, long actorUserId, CancellationToken cancellationToken);
    Task<PayrollManagementItemDto> AdjustPayrollRecordAsync(long payrollId, decimal actualDays, decimal bonusAmount, decimal deductions, CancellationToken cancellationToken);
}

public sealed class PayrollService(
    PayrollDbContext dbContext,
    AuthDbContext authDbContext) : IPayrollService
{
    // Mức lương cơ sở hiện hành 2.340.000đ -> Trần đóng BHXH/BHYT (20 lần) = 46.800.000đ
    private const decimal MaxInsuranceSalary = 46_800_000m;
    // Giảm trừ gia cảnh bản thân theo Luật thuế TNCN: 11.000.000đ/tháng
    private const decimal PersonalDeduction = 11_000_000m;

    public async Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(long userId, short? year, long? targetEmployeeId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, targetEmployeeId, cancellationToken);
        var targetYear = year ?? (short)DateTime.UtcNow.Year;

        // 1. Lấy danh sách các kỳ đã chốt trong database của nhân viên
        var query = dbContext.Payrolls.AsNoTracking().Where(p => p.EmployeeId == employeeId);
        if (year.HasValue)
        {
            query = query.Where(p => p.YearPeriod == targetYear);
        }

        var dbRecords = await query
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
                p.NetSalary,
                p.StandardWorkingDays,
                p.SnapshotBaseSalary,
                p.SnapshotPositionAllowance,
                p.SnapshotOtherAllowance,
                p.BonusAmount,
                p.Deductions))
            .ToListAsync(cancellationToken);

        // 2. Nếu đã có dữ liệu hoặc không chọn cụ thể năm, trả về danh sách DB
        if (dbRecords.Count > 0 && !year.HasValue)
        {
            return dbRecords;
        }

        // 3. Nếu chọn một năm cụ thể, đảm bảo có đủ 12 tháng (tháng nào có trong DB lấy DB, tháng nào chưa có thì tính chuẩn công thức thời gian thực)
        var resultList = new List<PayslipSummaryDto>();
        var dbMap = dbRecords.ToDictionary(r => r.MonthPeriod);

        for (short m = 12; m >= 1; m--)
        {
            if (dbMap.TryGetValue(m, out var existing))
            {
                resultList.Add(existing);
            }
            else
            {
                // Tính toán động theo công thức cho tháng chưa chốt
                var computed = await CalculateEmployeePayslipOnTheFlyAsync(employeeId, m, targetYear, cancellationToken);
                resultList.Add(new PayslipSummaryDto(
                    0,
                    employeeId,
                    m,
                    targetYear,
                    computed.ActualDays,
                    computed.GrossSalary,
                    computed.BhxhDeduct,
                    computed.TaxDeduct,
                    computed.NetSalary,
                    computed.StandardWorkingDays,
                    computed.SnapshotBaseSalary,
                    computed.SnapshotPositionAllowance,
                    computed.SnapshotOtherAllowance,
                    computed.BonusAmount,
                    computed.Deductions
                ));
            }
        }

        return resultList;
    }

    public async Task<PayslipDetailDto> GetPayslipForPeriodAsync(long userId, short month, short year, long? targetEmployeeId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, targetEmployeeId, cancellationToken);

        // 1. Kiểm tra xem kỳ này đã được tính/chốt hoặc điều chỉnh trong database chưa
        var existing = await dbContext.Payrolls.AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.MonthPeriod == month && p.YearPeriod == year, cancellationToken);

        if (existing is not null)
        {
            return new PayslipDetailDto(
                Id: existing.Id,
                EmployeeId: existing.EmployeeId,
                MonthPeriod: existing.MonthPeriod,
                YearPeriod: existing.YearPeriod,
                ActualDays: existing.ActualDays,
                GrossSalary: existing.GrossSalary,
                BhxhDeduct: existing.BhxhDeduct,
                TaxDeduct: existing.TaxDeduct,
                NetSalary: existing.NetSalary,
                StandardWorkingDays: existing.StandardWorkingDays,
                SnapshotBaseSalary: existing.SnapshotBaseSalary,
                SnapshotPositionAllowance: existing.SnapshotPositionAllowance,
                SnapshotOtherAllowance: existing.SnapshotOtherAllowance,
                BonusAmount: existing.BonusAmount,
                Deductions: existing.Deductions
            );
        }

        // 2. Nếu chưa chốt trong DB, tính toán chuẩn thời gian thực theo chấm công + hồ sơ lương
        return await CalculateEmployeePayslipOnTheFlyAsync(employeeId, month, year, cancellationToken);
    }

    private async Task<PayslipDetailDto> CalculateEmployeePayslipOnTheFlyAsync(long employeeId, short month, short year, CancellationToken cancellationToken)
    {
        var emp = await authDbContext.Employees.AsNoTracking()
            .Include(e => e.SalaryProfiles)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        const decimal standardDays = 22m;

        // Ngày công thực tế từ Chấm công + Đơn nghỉ phép có lương
        var attendanceCount = await dbContext.Attendances.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= periodStart && a.WorkDate <= periodEnd)
            .Select(a => a.WorkDate)
            .Distinct()
            .CountAsync(cancellationToken);

        var approvedPaidLeaveDays = await dbContext.Leaves.AsNoTracking()
            .Where(l => l.EmployeeId == employeeId
                     && l.Status == "APPROVED"
                     && (l.LeaveType == "ANNUAL" || l.LeaveType == "PAID")
                     && l.StartDate <= periodEnd && l.EndDate >= periodStart)
            .SumAsync(l => (decimal?)l.DaysCount, cancellationToken) ?? 0m;

        var totalRecordedDays = attendanceCount + approvedPaidLeaveDays;
        var actualDays = totalRecordedDays > 0 ? Math.Min(totalRecordedDays, standardDays) : standardDays;

        // Tìm hồ sơ lương áp dụng
        var activeProfile = emp?.SalaryProfiles
            .Where(sp => sp.EffectiveDate <= periodEnd && (sp.ExpiryDate == null || sp.ExpiryDate >= periodStart))
            .OrderByDescending(sp => sp.IsCurrent)
            .ThenByDescending(sp => sp.EffectiveDate)
            .FirstOrDefault();

        var baseSalary = activeProfile?.BaseSalary ?? 10_000_000m;
        var posAllowance = activeProfile?.PositionAllowance ?? 0m;
        var otherAllowance = activeProfile?.OtherAllowance ?? 0m;

        // Lương Gross tính theo tỷ lệ ngày công thực tế (BR-PAY-002: (Base + Pos) / 22 * ActualDays + Other)
        var proratedSalary = Math.Round((baseSalary + posAllowance) / standardDays * actualDays, 0);
        var gross = proratedSalary + otherAllowance;

        // BHXH 10.5% (trần 46.8tr)
        var insuranceSalary = activeProfile?.InsuranceSalary ?? (baseSalary + posAllowance);
        var subjectToInsurance = Math.Min(insuranceSalary, MaxInsuranceSalary);
        var bhxh = Math.Round(subjectToInsurance * 0.105m, 0);

        // Thu nhập chịu thuế TNCN = Gross - BHXH - Giảm trừ gia cảnh 11tr
        var taxableIncome = Math.Max(0m, gross - PersonalDeduction - bhxh);
        var tax = CalculateProgressivePersonalIncomeTax(taxableIncome);
        var net = Math.Max(0m, gross - bhxh - tax);

        return new PayslipDetailDto(
            Id: 0,
            EmployeeId: employeeId,
            MonthPeriod: month,
            YearPeriod: year,
            ActualDays: actualDays,
            GrossSalary: gross,
            BhxhDeduct: bhxh,
            TaxDeduct: tax,
            NetSalary: net,
            StandardWorkingDays: standardDays,
            SnapshotBaseSalary: baseSalary,
            SnapshotPositionAllowance: posAllowance,
            SnapshotOtherAllowance: otherAllowance,
            BonusAmount: 0m,
            Deductions: 0m
        );
    }

    public async Task<PayslipDetailDto> GetPayslipDetailAsync(long userId, long payrollId, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, null, cancellationToken);

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
            NetSalary: payroll.NetSalary,
            StandardWorkingDays: payroll.StandardWorkingDays,
            SnapshotBaseSalary: payroll.SnapshotBaseSalary,
            SnapshotPositionAllowance: payroll.SnapshotPositionAllowance,
            SnapshotOtherAllowance: payroll.SnapshotOtherAllowance,
            BonusAmount: payroll.BonusAmount,
            Deductions: payroll.Deductions
        );
    }

    public async Task<YearlyPayrollSummaryDto> GetMyYearlyPayslipsAsync(long userId, short year, CancellationToken cancellationToken)
    {
        var employeeId = await GetEmployeeIdAsync(userId, null, cancellationToken);

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
                p.NetSalary,
                p.StandardWorkingDays,
                p.SnapshotBaseSalary,
                p.SnapshotPositionAllowance,
                p.SnapshotOtherAllowance,
                p.BonusAmount,
                p.Deductions))
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

    private async Task<long> GetEmployeeIdAsync(long userId, long? targetEmployeeId, CancellationToken cancellationToken)
    {
        if (targetEmployeeId.HasValue && targetEmployeeId.Value > 0)
        {
            return targetEmployeeId.Value;
        }

        var user = await authDbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new DomainException("AUTH_USER_NOT_FOUND", "Không tìm thấy thông tin tài khoản.");
        }

        if (!user.EmployeeId.HasValue)
        {
            // Fallback lấy nhân viên đầu tiên để admin/dev test nhanh chóng
            var firstEmp = await authDbContext.Employees.AsNoTracking()
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (firstEmp is not null)
            {
                return firstEmp.Id;
            }

            throw new DomainException("EMPLOYEE_NOT_APPLICABLE", "Tài khoản Quản trị Hệ thống không có bảng lương.");
        }

        return user.EmployeeId.Value;
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
                p.NetSalary,
                p.StandardWorkingDays,
                p.SnapshotBaseSalary,
                p.SnapshotPositionAllowance,
                p.SnapshotOtherAllowance,
                p.BonusAmount,
                p.Deductions
            );
        }).ToList();
    }

    public async Task<int> CalculatePeriodPayrollAsync(short month, short year, long actorUserId, CancellationToken cancellationToken)
    {
        var employees = await authDbContext.Employees.AsNoTracking()
            .Include(e => e.Position)
            .Include(e => e.SalaryProfiles)
            .Where(e => e.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        if (employees.Count == 0) return 0;

        var processedCount = 0;
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        const decimal standardDays = 22m;

        foreach (var emp in employees)
        {
            var existing = await dbContext.Payrolls
                .FirstOrDefaultAsync(p => p.EmployeeId == emp.Id && p.MonthPeriod == month && p.YearPeriod == year, cancellationToken);

            // 1. Tự động tính số ngày công thực tế từ Chấm công + Nghỉ phép hưởng lương đã duyệt
            var attendanceCount = await dbContext.Attendances.AsNoTracking()
                .Where(a => a.EmployeeId == emp.Id && a.WorkDate >= periodStart && a.WorkDate <= periodEnd)
                .Select(a => a.WorkDate)
                .Distinct()
                .CountAsync(cancellationToken);

            var approvedPaidLeaveDays = await dbContext.Leaves.AsNoTracking()
                .Where(l => l.EmployeeId == emp.Id 
                         && l.Status == "APPROVED" 
                         && (l.LeaveType == "ANNUAL" || l.LeaveType == "PAID")
                         && l.StartDate <= periodEnd && l.EndDate >= periodStart)
                .SumAsync(l => (decimal?)l.DaysCount, cancellationToken) ?? 0m;

            var totalRecordedDays = attendanceCount + approvedPaidLeaveDays;
            
            // Nếu hệ thống đã có dữ liệu chấm công hoặc nghỉ phép thì tính theo thực tế;
            // Nếu chưa có (môi trường test) thì mặc định tính tròn 22 ngày công chuẩn
            var actualDays = totalRecordedDays > 0 ? Math.Min(totalRecordedDays, standardDays) : standardDays;

            // Nếu HR đã chủ động điều chỉnh số ngày công trước đó (và chưa có log điểm danh) thì bảo lưu
            if (existing is not null && existing.ActualDays > 0 && totalRecordedDays == 0)
            {
                actualDays = existing.ActualDays;
            }

            // 2. Tìm hồ sơ lương áp dụng cho kỳ tính lương (chuẩn 3NF temporal lookup)
            var activeProfile = emp.SalaryProfiles
                .Where(sp => sp.EffectiveDate <= periodEnd && (sp.ExpiryDate == null || sp.ExpiryDate >= periodStart))
                .OrderByDescending(sp => sp.IsCurrent)
                .ThenByDescending(sp => sp.EffectiveDate)
                .FirstOrDefault();

            var baseSalary = activeProfile?.BaseSalary ?? 10_000_000m;
            var posAllowance = activeProfile?.PositionAllowance ?? 0m;
            var otherAllowance = activeProfile?.OtherAllowance ?? 0m;

            // Lương vị trí tính theo tỷ lệ ngày công thực tế (BR-PAY-002: Base / 22 * ActualDays)
            var proratedSalary = Math.Round((baseSalary + posAllowance) / standardDays * actualDays, 0);
            var gross = proratedSalary + otherAllowance;

            // Bảo tồn tiền Thưởng (Bonus) và Khấu trừ (Deductions) nếu trước đó đã điều chỉnh
            var bonus = existing?.BonusAmount ?? 0m;
            var deductions = existing?.Deductions ?? 0m;

            // Đóng BHXH bắt buộc 10.5% (8% BHXH + 1.5% BHYT + 1% BHTN) có chặn trần tối đa 46.800.000đ
            var insuranceSalary = activeProfile?.InsuranceSalary ?? (baseSalary + posAllowance);
            var subjectToInsurance = Math.Min(insuranceSalary, MaxInsuranceSalary);
            var bhxh = Math.Round(subjectToInsurance * 0.105m, 0);

            // Thu nhập chịu thuế TNCN = (Gross + Thưởng) - BHXH - Giảm trừ gia cảnh bản thân (11tr)
            var totalIncome = gross + bonus;
            var taxableIncome = Math.Max(0m, totalIncome - PersonalDeduction - bhxh);

            // Thuế TNCN tính theo Biểu thuế lũy tiến từng phần 5 bậc mới
            var tax = CalculateProgressivePersonalIncomeTax(taxableIncome);

            // Lương Net thực lĩnh = (Gross + Thưởng) - Khấu trừ phạt - BHXH - Thuế TNCN
            var net = Math.Max(0m, totalIncome - deductions - bhxh - tax);

            if (existing is not null)
            {
                existing.ActualDays = actualDays;
                existing.StandardWorkingDays = standardDays;
                existing.SnapshotBaseSalary = baseSalary;
                existing.SnapshotPositionAllowance = posAllowance;
                existing.SnapshotOtherAllowance = otherAllowance;
                existing.GrossSalary = gross;
                existing.BonusAmount = bonus;
                existing.Deductions = deductions;
                existing.BhxhDeduct = bhxh;
                existing.TaxDeduct = tax;
                existing.NetSalary = net;
            }
            else
            {
                dbContext.Payrolls.Add(new PayrollRecord
                {
                    EmployeeId = emp.Id,
                    MonthPeriod = month,
                    YearPeriod = year,
                    ActualDays = actualDays,
                    StandardWorkingDays = standardDays,
                    SnapshotBaseSalary = baseSalary,
                    SnapshotPositionAllowance = posAllowance,
                    SnapshotOtherAllowance = otherAllowance,
                    GrossSalary = gross,
                    BonusAmount = bonus,
                    Deductions = deductions,
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

    public async Task<PayrollManagementItemDto> AdjustPayrollRecordAsync(
        long payrollId,
        decimal actualDays,
        decimal bonusAmount,
        decimal deductions,
        CancellationToken cancellationToken)
    {
        if (actualDays < 0 || bonusAmount < 0 || deductions < 0)
        {
            throw new DomainException("INVALID_AMOUNT", "Ngày công, tiền thưởng và khoản khấu trừ không được là số âm.");
        }

        var payroll = await dbContext.Payrolls
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            throw new DomainException("PAYROLL_NOT_FOUND", "Không tìm thấy bản ghi kỳ lương cần điều chỉnh.");
        }

        payroll.ActualDays = actualDays;
        payroll.BonusAmount = bonusAmount;
        payroll.Deductions = deductions;

        // Tính lại Gross theo số ngày công thực tế mới
        var standardDays = payroll.StandardWorkingDays > 0 ? payroll.StandardWorkingDays : 22m;
        var proratedSalary = Math.Round((payroll.SnapshotBaseSalary + payroll.SnapshotPositionAllowance) / standardDays * actualDays, 0);
        payroll.GrossSalary = proratedSalary + payroll.SnapshotOtherAllowance;

        // Đóng BHXH (có trần 46.8tr)
        var subjectToInsurance = Math.Min(payroll.SnapshotBaseSalary + payroll.SnapshotPositionAllowance, MaxInsuranceSalary);
        payroll.BhxhDeduct = Math.Round(subjectToInsurance * 0.105m, 0);

        // Tính lại thuế và Net sau khi thay đổi Ngày công, Thưởng & Khấu trừ
        var totalIncome = payroll.GrossSalary + payroll.BonusAmount;
        var taxableIncome = Math.Max(0m, totalIncome - PersonalDeduction - payroll.BhxhDeduct);
        payroll.TaxDeduct = CalculateProgressivePersonalIncomeTax(taxableIncome);
        payroll.NetSalary = Math.Max(0m, totalIncome - payroll.Deductions - payroll.BhxhDeduct - payroll.TaxDeduct);

        await dbContext.SaveChangesAsync(cancellationToken);

        var emp = await authDbContext.Employees.AsNoTracking()
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == payroll.EmployeeId, cancellationToken);

        return new PayrollManagementItemDto(
            payroll.Id,
            payroll.EmployeeId,
            emp?.Code ?? "--",
            emp?.FullName ?? "Nhân viên #" + payroll.EmployeeId,
            emp?.Department?.Name ?? "N/A",
            payroll.MonthPeriod,
            payroll.YearPeriod,
            payroll.ActualDays,
            payroll.GrossSalary,
            payroll.BhxhDeduct,
            payroll.TaxDeduct,
            payroll.NetSalary,
            payroll.StandardWorkingDays,
            payroll.SnapshotBaseSalary,
            payroll.SnapshotPositionAllowance,
            payroll.SnapshotOtherAllowance,
            payroll.BonusAmount,
            payroll.Deductions
        );
    }

    /// <summary>
    /// Biểu thuế lũy tiến từng phần 5 bậc mới (Dự thảo Luật Thuế TNCN cải cách)
    /// </summary>
    public static decimal CalculateProgressivePersonalIncomeTax(decimal taxableIncome)
    {
        if (taxableIncome <= 0m) return 0m;

        // Bậc 1: Đến 10 triệu đồng/tháng -> 5% (Số trừ cố định: 0)
        if (taxableIncome <= 10_000_000m)
            return Math.Round(taxableIncome * 0.05m, 0);

        // Bậc 2: Trên 10 triệu đến 30 triệu đồng/tháng -> 10% (Số trừ cố định: 500.000đ)
        if (taxableIncome <= 30_000_000m)
            return Math.Round(taxableIncome * 0.10m - 500_000m, 0);

        // Bậc 3: Trên 30 triệu đến 60 triệu đồng/tháng -> 20% (Số trừ cố định: 3.500.000đ)
        if (taxableIncome <= 60_000_000m)
            return Math.Round(taxableIncome * 0.20m - 3_500_000m, 0);

        // Bậc 4: Trên 60 triệu đến 100 triệu đồng/tháng -> 30% (Số trừ cố định: 9.500.000đ)
        if (taxableIncome <= 100_000_000m)
            return Math.Round(taxableIncome * 0.30m - 9_500_000m, 0);

        // Bậc 5: Trên 100 triệu đồng/tháng -> 35% (Số trừ cố định: 14.500.000đ)
        return Math.Round(taxableIncome * 0.35m - 14_500_000m, 0);
    }
}
