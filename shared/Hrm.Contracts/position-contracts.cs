namespace Hrm.Contracts;

public record PositionDto(
    long Id,
    string Code,
    string Title,
    decimal BaseSalary,
    decimal Allowance,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CreatePositionRequest(
    string Code,
    string Title,
    decimal BaseSalary,
    decimal Allowance,
    string? Description);

public record UpdatePositionRequest(
    string Title,
    decimal BaseSalary,
    decimal Allowance,
    string? Description,
    bool IsActive);
