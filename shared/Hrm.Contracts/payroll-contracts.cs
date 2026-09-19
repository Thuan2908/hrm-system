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
    decimal NetSalary
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
    decimal NetSalary
);
