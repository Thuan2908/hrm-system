namespace Hrm.Contracts;

public record DepartmentDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CreateDepartmentRequest(
    string Code,
    string Name,
    string? Description);

public record UpdateDepartmentRequest(
    string Name,
    string? Description,
    bool IsActive);
