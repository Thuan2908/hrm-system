namespace Hrm.Contracts;

public record EmployeeDto(
    long Id,
    string Code,
    string FullName,
    long DepartmentId,
    string DepartmentName,
    long PositionId,
    string PositionTitle,
    string Email,
    string Phone,
    string? Address,
    DateTime HireDate,
    string Status,
    string? DepartmentCode,
    string? PositionCode,
    DateTimeOffset CreatedAt);

public record CreateEmployeeRequest(
    string FullName,
    long DepartmentId,
    long PositionId,
    string Email,
    string Phone,
    string? Address,
    DateTime HireDate);

public record UpdateEmployeeRequest(
    string FullName,
    long DepartmentId,
    long PositionId,
    string Email,
    string Phone,
    string? Address,
    DateTime HireDate);

public record TransferEmployeeRequest(
    long TargetDepartmentId,
    string Reason);

public record PromoteEmployeeRequest(
    long TargetPositionId,
    string Reason);

public record EmployeeTimelineEventDto(
    long Id,
    long EmployeeId,
    string EventType,
    string Title,
    string Description,
    DateTimeOffset Timestamp,
    string? CreatedByName);
