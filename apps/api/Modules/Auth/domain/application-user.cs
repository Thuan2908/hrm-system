namespace Hrm.Modules.Auth.Domain;

public sealed class ApplicationUser
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public long RoleId { get; set; }
    public required string UserName { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public ApplicationRole Role { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public UserSecurityState? SecurityState { get; set; }
    public UserAccountMetadata? Metadata { get; set; }
}

public sealed class ApplicationRole
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public sealed class Position
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public required string Name { get; set; }
    public string? Title { get; set; }
}

public sealed class Employee
{
    public long Id { get; set; }
    public long DepartmentId { get; set; }
    public long? PositionId { get; set; }
    public required string Code { get; set; }
    public required string FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? EducationLevel { get; set; }
    public decimal BaseSalary { get; set; }
    public DateOnly? JoinDate { get; set; }
    public DateOnly? HireDate { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTimeOffset CreatedAt { get; set; }

    public Department Department { get; set; } = null!;
    public Position? Position { get; set; }
}

public sealed class Department
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}

public sealed class UserSecurityState
{
    public long UserId { get; set; }
    public int FailedAccessCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public ApplicationUser User { get; set; } = null!;
}

public sealed class UserAccountMetadata
{
    public long UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
