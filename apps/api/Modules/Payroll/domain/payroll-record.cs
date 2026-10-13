namespace Hrm.Modules.Payroll.Domain;

public sealed class PayrollRecord
{
    public long Id { get; set; } // payroll_id
    public long EmployeeId { get; set; } // emp_id
    public short MonthPeriod { get; set; } // month_period
    public short YearPeriod { get; set; } // year_period
    public decimal ActualDays { get; set; } // actual_days
    public decimal StandardWorkingDays { get; set; } = 22m; // standard_working_days
    public decimal SnapshotBaseSalary { get; set; } // snapshot_base_salary
    public decimal SnapshotPositionAllowance { get; set; } // snapshot_position_allowance
    public decimal SnapshotOtherAllowance { get; set; } // snapshot_other_allowance
    public decimal GrossSalary { get; set; } // gross_sal
    public decimal BhxhDeduct { get; set; } // bhxh_deduct
    public decimal TaxDeduct { get; set; } // tax_deduct
    public decimal NetSalary { get; set; } // net_sal
}
