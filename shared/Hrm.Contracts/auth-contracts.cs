namespace Hrm.Contracts;

public sealed record LoginRequest(string UserName, string Password, string? DeviceId = null);

public sealed record HeartbeatRequest(string? DeviceId = null);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    UserSessionDto User);

public sealed record UserSessionDto(
    long Id,
    string UserName,
    string FullName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record UpdateProfileRequest(
    string FullName,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    DateOnly? DateOfBirth = null,
    string? Gender = null);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record EmployeeProfileDto(
    long EmployeeId,
    string EmployeeCode,
    string FullName,
    string UserName,
    string RoleName,
    string DepartmentName,
    string DepartmentCode,
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
    DateTimeOffset CreatedAt);

public sealed record AccountStatusDto(
    bool IsActive,
    bool IsLocked,
    bool IsResigned,
    bool CanAccess,
    string Reason,
    string Message);
