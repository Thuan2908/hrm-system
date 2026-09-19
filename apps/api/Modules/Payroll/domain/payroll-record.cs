namespace Hrm.Modules.Payroll.Domain;

public sealed class PayrollRecord
{
    public long Id { get; set; } // payroll_id
    public long EmployeeId { get; set; } // emp_id
    public short MonthPeriod { get; set; } // month_period
    public short YearPeriod { get; set; } // year_period
    public decimal ActualDays { get; set; } // actual_days
    public decimal GrossSalary { get; set; } // gross_sal
    public decimal BhxhDeduct { get; set; } // bhxh_deduct
    public decimal TaxDeduct { get; set; } // tax_deduct
    public decimal NetSalary { get; set; } // net_sal
}
