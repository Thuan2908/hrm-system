namespace Hrm.Contracts;

public sealed record PayslipSummaryDto(
    long Id,
    long EmployeeId,
    short MonthPeriod,
    short YearPeriod,
    decimal ActualDays,
    decimal GrossSalary,
    decimal BhxhDeduct,
    decimal TaxDeduct,
    decimal NetSalary,
    decimal StandardWorkingDays = 22m,
    decimal SnapshotBaseSalary = 0m,
    decimal SnapshotPositionAllowance = 0m,
    decimal SnapshotOtherAllowance = 0m,
    decimal BonusAmount = 0m,
    decimal Deductions = 0m
);

public sealed record PayslipDetailDto(
    long Id,
    long EmployeeId,
    short MonthPeriod,
    short YearPeriod,
    decimal ActualDays,
    decimal GrossSalary,
    decimal BhxhDeduct,
    decimal TaxDeduct,
    decimal NetSalary,
    decimal StandardWorkingDays = 22m,
    decimal SnapshotBaseSalary = 0m,
    decimal SnapshotPositionAllowance = 0m,
    decimal SnapshotOtherAllowance = 0m,
    decimal BonusAmount = 0m,
    decimal Deductions = 0m
);

public sealed record CalculatePayrollRequest(
    short MonthPeriod,
    short YearPeriod
);

public sealed record AdjustPayrollRecordRequest(
    long PayrollId,
    decimal ActualDays,
    decimal BonusAmount,
    decimal Deductions
);

public sealed record PayrollManagementItemDto(
    long Id,
    long EmployeeId,
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    short MonthPeriod,
    short YearPeriod,
    decimal ActualDays,
    decimal GrossSalary,
    decimal BhxhDeduct,
    decimal TaxDeduct,
    decimal NetSalary,
    decimal StandardWorkingDays = 22m,
    decimal SnapshotBaseSalary = 0m,
    decimal SnapshotPositionAllowance = 0m,
    decimal SnapshotOtherAllowance = 0m,
    decimal BonusAmount = 0m,
    decimal Deductions = 0m
);

public sealed record YearlyPayrollSummaryDto(
    short YearPeriod,
    decimal TotalActualDays,
    decimal TotalGrossSalary,
    decimal TotalBhxhDeduct,
    decimal TotalTaxDeduct,
    decimal TotalNetSalary,
    IReadOnlyList<PayslipSummaryDto> MonthlyRecords
);
