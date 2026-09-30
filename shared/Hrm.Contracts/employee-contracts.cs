namespace Hrm.Contracts;

public sealed record EmployeeDto(
    long Id,
    string Code,
    string FullName,
    long DepartmentId,
    string DepartmentName,
    long? PositionId,
    string? PositionName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Phone,
    string? Email,
    string? Address,
    string? EducationLevel,
    decimal BaseSalary,
    DateOnly? JoinDate,
    DateOnly? HireDate,
    string Status,
    bool HasAccount,
    DateTimeOffset CreatedAt);

public sealed record CreateEmployeeRequest(
    string FullName,
    string? Code,
    long DepartmentId,
    long? PositionId,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Phone,
    string? Email,
    string? Address,
    string? EducationLevel,
    decimal BaseSalary,
    DateOnly? JoinDate,
    DateOnly? HireDate,
    bool CreateAccount = false,
    string? UserName = null,
    string? Password = null,
    string? Role = null);

public sealed record UpdateEmployeeRequest(
    string FullName,
    long DepartmentId,
    long? PositionId,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Phone,
    string? Email,
    string? Address,
    string? EducationLevel,
    decimal BaseSalary,
    DateOnly? JoinDate,
    DateOnly? HireDate,
    string Status);

public sealed record DepartmentDto(
    long Id,
    string Code,
    string Name);

public sealed record PositionDto(
    long Id,
    string? Code,
    string Name,
    string? Title,
    decimal AllowanceRate);
