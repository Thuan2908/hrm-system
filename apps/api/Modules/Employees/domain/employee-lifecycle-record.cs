namespace Hrm.Modules.Employees.Domain;

public sealed class EmployeeLifecycleRecord
{
    public long Id { get; set; }
    public required string Status { get; set; }
}
