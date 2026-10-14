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
    string? Gender = null,
    string? BankName = null,
    string? BankAccountNumber = null);

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
    DateOnly? HireDate,
    string Status,
    DateTimeOffset CreatedAt,
    decimal PositionAllowance = 0m,
    decimal OtherAllowance = 0m,
    decimal? InsuranceSalary = null,
    int DependentCount = 0,
    string? BankName = null,
    string? BankAccountNumber = null);

public sealed record AccountStatusDto(
    bool IsActive,
    bool IsLocked,
    bool IsResigned,
    bool CanAccess,
    string Reason,
    string Message);

public static class PasswordPolicy
{
    public const int MinLength = 3;
    public const int BcryptWorkFactor = 12;

    public static bool IsValid(string? password, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            errorMessage = "Mật khẩu phải có ít nhất 3 ký tự.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

