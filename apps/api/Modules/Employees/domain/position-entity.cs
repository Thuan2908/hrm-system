namespace Hrm.Modules.Employees.Domain;

public sealed class PositionEntity
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
