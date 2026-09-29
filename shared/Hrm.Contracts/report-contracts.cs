namespace Hrm.Contracts;

public sealed record HrDashboardSummaryDto(
    int TotalEmployees,
    int ActiveEmployees,
    int ResignedEmployees,
    int TotalDepartments,
    decimal TotalMonthlyPayroll,
    int PendingLeaveRequests,
    int TodayAttendanceCount
);

public sealed record DepartmentDistributionDto(
    string DepartmentName,
    int EmployeeCount,
    decimal Percentage
);

public sealed record EducationDistributionDto(
    string EducationLevel,
    int Count,
    decimal Percentage
);

public sealed record HeadcountReportDto(
    int TotalEmployees,
    int ActiveCount,
    int OnLeaveCount,
    int ResignedCount,
    IReadOnlyList<DepartmentDistributionDto> Departments,
    IReadOnlyList<EducationDistributionDto> EducationLevels
);

public sealed record TenureGroupDto(
    string GroupLabel,
    int Count,
    decimal Percentage
);

public sealed record TenureReportDto(
    double AverageTenureYears,
    IReadOnlyList<TenureGroupDto> Groups
);

public sealed record AttendanceReportDto(
    int Month,
    int Year,
    int TotalCheckIns,
    int PresentCount,
    int LateCount,
    int ApprovedLeaveDays,
    int PendingLeaveCount
);

public sealed record SalaryRangeDto(
    string RangeLabel,
    int EmployeeCount,
    decimal Percentage
);

public sealed record DepartmentSalaryDto(
    string DepartmentName,
    decimal AverageSalary,
    decimal TotalSalary
);

public sealed record PayrollReportDto(
    int Month,
    int Year,
    decimal TotalGrossSalary,
    decimal TotalNetSalary,
    decimal TotalInsuranceDeductions,
    decimal TotalTaxDeductions,
    IReadOnlyList<SalaryRangeDto> SalaryRanges,
    IReadOnlyList<DepartmentSalaryDto> DepartmentSalaries
);
