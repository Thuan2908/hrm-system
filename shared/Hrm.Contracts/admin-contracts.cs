namespace Hrm.Contracts;

public sealed record AdminUserDto(
    long Id,
    long EmployeeId,
    string UserName,
    string FullName,
    string Department,
    bool IsActive,
    bool IsLocked,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyCollection<string> Roles);

public sealed record CreateAdminUserRequest(
    long EmployeeId,
    string UserName,
    string Password,
    IReadOnlyCollection<string> Roles);

public sealed record UpdateAdminUserRequest(long EmployeeId);

public sealed record EmployeeAccountOptionDto(
    long Id,
    string Code,
    string FullName,
    string Department,
    bool HasAccount);

public sealed record SetAccountStatusRequest(bool IsActive);

public sealed record SetAccountLockRequest(bool IsLocked);

public sealed record SetUserRolesRequest(IReadOnlyCollection<string> Roles);

public sealed record ResetUserPasswordRequest(string NewPassword);

public sealed record RoleDto(
    long Id,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyCollection<string> Permissions);

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);

public sealed record SetRolePermissionsRequest(IReadOnlyCollection<string> Permissions);

public sealed record AuditLogDto(
    Guid Id,
    long? ActorUserId,
    string Action,
    string EntityType,
    string EntityId,
    string? BeforeJson,
    string? AfterJson,
    DateTimeOffset CreatedAt);
