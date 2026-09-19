namespace Hrm.Modules.Employees.Domain;

public sealed class EmployeeEntity
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? FullName { get; set; }
    public long? DepartmentId { get; set; }
    public DepartmentEntity? Department { get; set; }
    public long? PositionId { get; set; }
    public PositionEntity? Position { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? JoinDate { get; set; }
    public decimal BaseSalary { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
