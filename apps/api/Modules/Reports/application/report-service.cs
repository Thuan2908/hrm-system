using Hrm.Contracts;
using Hrm.Modules.Reports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Reports.Application;

public interface IReportService
{
    Task<HrDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken);
    Task<HeadcountReportDto> GetHeadcountReportAsync(CancellationToken cancellationToken);
    Task<TenureReportDto> GetTenureReportAsync(CancellationToken cancellationToken);
    Task<AttendanceReportDto> GetAttendanceReportAsync(int? month, int? year, CancellationToken cancellationToken);
    Task<PayrollReportDto> GetPayrollReportAsync(int? month, int? year, CancellationToken cancellationToken);
}

public sealed class ReportService(ReportsDbContext dbContext) : IReportService
{
    public async Task<HrDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        var totalEmployees = await dbContext.Employees.CountAsync(cancellationToken);
        var activeEmployees = await dbContext.Employees
            .CountAsync(e => e.Status == "WORKING" || e.Status == "ACTIVE", cancellationToken);
        var resignedEmployees = await dbContext.Employees
            .CountAsync(e => e.Status == "RESIGNED" || e.Status == "INACTIVE", cancellationToken);
        var totalDepartments = await dbContext.Departments.CountAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var currentYear = now.Year;

        var monthlyPayroll = await dbContext.Payrolls
            .Where(p => p.MonthPeriod == currentMonth && p.YearPeriod == currentYear)
            .SumAsync(p => (decimal?)p.NetSalary, cancellationToken) ?? 0m;

        if (monthlyPayroll == 0m)
        {
            var activeEmpIds = await dbContext.Employees
                .Where(e => e.Status == "WORKING" || e.Status == "ACTIVE")
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);

            monthlyPayroll = await dbContext.SalaryProfiles
                .Where(sp => sp.IsCurrent && activeEmpIds.Contains(sp.EmployeeId))
                .SumAsync(sp => (decimal?)sp.BaseSalary, cancellationToken) ?? 0m;
        }

        var pendingLeaveRequests = await dbContext.Leaves
            .CountAsync(l => l.Status == "PENDING", cancellationToken);

        var today = DateOnly.FromDateTime(now);
        var todayAttendanceCount = await dbContext.Attendances
            .CountAsync(a => a.WorkDate == today, cancellationToken);

        return new HrDashboardSummaryDto(
            totalEmployees,
            activeEmployees,
            resignedEmployees,
            totalDepartments,
            monthlyPayroll,
            pendingLeaveRequests,
            todayAttendanceCount);
    }

    public async Task<HeadcountReportDto> GetHeadcountReportAsync(CancellationToken cancellationToken)
    {
        var employees = await dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .ToListAsync(cancellationToken);

        var total = employees.Count;
        var active = employees.Count(e => e.Status == "WORKING" || e.Status == "ACTIVE");
        var onLeave = employees.Count(e => e.Status == "ON_LEAVE");
        var resigned = employees.Count(e => e.Status == "RESIGNED" || e.Status == "INACTIVE");

        var departments = employees
            .GroupBy(e => e.Department?.Name ?? "Chưa phân bổ")
            .Select(g => new DepartmentDistributionDto(
                g.Key,
                g.Count(),
                total > 0 ? Math.Round((decimal)g.Count() * 100 / total, 1) : 0m))
            .OrderByDescending(d => d.EmployeeCount)
            .ToList();

        var educations = employees
            .GroupBy(e => EducationLevelCatalog.ToDisplayName(e.EducationLevel))
            .Select(g => new EducationDistributionDto(
                g.Key,
                g.Count(),
                total > 0 ? Math.Round((decimal)g.Count() * 100 / total, 1) : 0m))
            .OrderByDescending(e => e.Count)
            .ToList();

        return new HeadcountReportDto(
            total,
            active,
            onLeave,
            resigned,
            departments,
            educations);
    }

    public async Task<TenureReportDto> GetTenureReportAsync(CancellationToken cancellationToken)
    {
        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Status == "WORKING" || e.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var total = employees.Count;

        var lessThan1Year = 0;
        var oneToThreeYears = 0;
        var threeToFiveYears = 0;
        var over5Years = 0;
        var totalDays = 0d;

        foreach (var emp in employees)
        {
            var startDate = emp.HireDate ?? DateOnly.FromDateTime(emp.CreatedAt.UtcDateTime);
            var days = Math.Max(0, today.DayNumber - startDate.DayNumber);
            totalDays += days;

            var years = days / 365.25;
            if (years < 1)
            {
                lessThan1Year++;
            }
            else if (years < 3)
            {
                oneToThreeYears++;
            }
            else if (years < 5)
            {
                threeToFiveYears++;
            }
            else
            {
                over5Years++;
            }
        }

        var avgTenureYears = total > 0 ? Math.Round(totalDays / (total * 365.25), 1) : 0;

        var groups = new List<TenureGroupDto>
        {
            new("< 1 năm", lessThan1Year, total > 0 ? Math.Round((decimal)lessThan1Year * 100 / total, 1) : 0m),
            new("1 - 3 năm", oneToThreeYears, total > 0 ? Math.Round((decimal)oneToThreeYears * 100 / total, 1) : 0m),
            new("3 - 5 năm", threeToFiveYears, total > 0 ? Math.Round((decimal)threeToFiveYears * 100 / total, 1) : 0m),
            new("> 5 năm", over5Years, total > 0 ? Math.Round((decimal)over5Years * 100 / total, 1) : 0m)
        };

        return new TenureReportDto(avgTenureYears, groups);
    }

    public async Task<AttendanceReportDto> GetAttendanceReportAsync(int? month, int? year, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;

        var attendances = await dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.WorkDate.Month == targetMonth && a.WorkDate.Year == targetYear)
            .ToListAsync(cancellationToken);

        var totalCheckIns = attendances.Count;
        var presentCount = attendances.Count(a => a.Status == "PRESENT" || a.Status == "ON_TIME");
        var lateCount = attendances.Count(a => a.Status == "LATE");

        var leaves = await dbContext.Leaves
            .AsNoTracking()
            .Where(l => l.StartDate.Month == targetMonth && l.StartDate.Year == targetYear)
            .ToListAsync(cancellationToken);

        var approvedLeaveDays = (int)leaves.Where(l => l.Status == "APPROVED").Sum(l => l.DaysCount);
        var pendingLeaveCount = leaves.Count(l => l.Status == "PENDING");

        return new AttendanceReportDto(
            targetMonth,
            targetYear,
            totalCheckIns,
            presentCount,
            lateCount,
            approvedLeaveDays,
            pendingLeaveCount);
    }

    public async Task<PayrollReportDto> GetPayrollReportAsync(int? month, int? year, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;

        var payrolls = await dbContext.Payrolls
            .AsNoTracking()
            .Where(p => p.MonthPeriod == targetMonth && p.YearPeriod == targetYear)
            .ToListAsync(cancellationToken);

        var gross = payrolls.Sum(p => p.GrossSalary);
        var net = payrolls.Sum(p => p.NetSalary);
        var insurance = payrolls.Sum(p => p.BhxhDeduct);
        var tax = payrolls.Sum(p => p.TaxDeduct);

        // Lấy danh sách lương từ SalaryProfiles hiện hành (chuẩn 3NF) hoặc fallback sang bảng employees
        var activeEmpIds = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Status == "WORKING" || e.Status == "ACTIVE")
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var currentProfiles = await dbContext.SalaryProfiles
            .AsNoTracking()
            .Where(sp => sp.IsCurrent && activeEmpIds.Contains(sp.EmployeeId))
            .ToListAsync(cancellationToken);

        var salaries = currentProfiles.Select(sp => sp.BaseSalary).ToList();

        // Phân bổ mức lương theo 3 nhóm chuẩn đề bài: Dưới 10tr, 10-20tr, Trên 20tr
        var rangeUnder10 = 0;
        var range10To20 = 0;
        var rangeOver20 = 0;

        var totalSalaryCount = salaries.Count;
        foreach (var s in salaries)
        {
            if (s < 10_000_000m) rangeUnder10++;
            else if (s <= 20_000_000m) range10To20++;
            else rangeOver20++;
        }

        var ranges = new List<SalaryRangeDto>
        {
            new("Dưới 10 triệu", rangeUnder10, totalSalaryCount > 0 ? Math.Round((decimal)rangeUnder10 * 100 / totalSalaryCount, 1) : 0m),
            new("10 - 20 triệu", range10To20, totalSalaryCount > 0 ? Math.Round((decimal)range10To20 * 100 / totalSalaryCount, 1) : 0m),
            new("Trên 20 triệu", rangeOver20, totalSalaryCount > 0 ? Math.Round((decimal)rangeOver20 * 100 / totalSalaryCount, 1) : 0m)
        };

        // Department breakdown
        var empWithDept = await dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e => (e.Status == "WORKING" || e.Status == "ACTIVE"))
            .ToListAsync(cancellationToken);

        var salaryMap = currentProfiles.ToDictionary(sp => sp.EmployeeId, sp => sp.BaseSalary);

        var deptSalaries = empWithDept
            .GroupBy(e => e.Department?.Name ?? "Chưa phân bổ")
            .Select(g =>
            {
                var deptSals = g.Select(e => salaryMap.TryGetValue(e.Id, out var sal) ? sal : 0m).ToList();
                var avg = deptSals.Count > 0 ? Math.Round(deptSals.Average(), 0) : 0m;
                var totalSal = deptSals.Sum();
                return new DepartmentSalaryDto(g.Key, avg, totalSal);
            })
            .OrderByDescending(d => d.TotalSalary)
            .ToList();

        return new PayrollReportDto(
            targetMonth,
            targetYear,
            gross > 0 ? gross : salaries.Sum(),
            net > 0 ? net : salaries.Sum() * 0.895m,
            insurance > 0 ? insurance : salaries.Sum() * 0.105m,
            tax,
            ranges,
            deptSalaries);
    }
}
