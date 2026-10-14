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
    DateOnly? HireDate,
    string Status,
    bool HasAccount,
    DateTimeOffset CreatedAt,
    long? ManagerId = null,
    decimal PositionAllowance = 0m,
    decimal OtherAllowance = 0m,
    decimal? InsuranceSalary = null,
    int DependentCount = 0,
    string? BankName = null,
    string? BankAccountNumber = null);

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
    DateOnly? HireDate,
    string Status);

public sealed record DepartmentDto(
    long Id,
    string Code,
    string Name,
    string? Description = null,
    bool IsActive = true,
    int EmployeeCount = 0);

public sealed record CreateDepartmentRequest(
    string Code,
    string Name,
    string? Description = null);

public sealed record UpdateDepartmentRequest(
    string Code,
    string Name,
    string? Description = null,
    bool IsActive = true);

public sealed record PositionDto(
    long Id,
    string? Code,
    string Name,
    long? DepartmentId = null,
    string? DepartmentName = null,
    decimal DefaultPositionAllowance = 0m,
    bool IsActive = true);

public sealed record CreatePositionRequest(
    string Code,
    string Name,
    long? DepartmentId = null,
    decimal DefaultPositionAllowance = 0m);

public sealed record UpdatePositionRequest(
    string Code,
    string Name,
    long? DepartmentId = null,
    decimal DefaultPositionAllowance = 0m,
    bool IsActive = true);

public sealed record TransferDepartmentRequest(
    long TargetDepartmentId,
    long? TargetPositionId = null,
    string? Reason = null,
    DateOnly? EffectiveDate = null);

public sealed record PromoteEmployeeRequest(
    long TargetPositionId,
    decimal? NewBaseSalary = null,
    decimal? NewPositionAllowance = null,
    string? NewRoleName = null,
    string? Reason = null,
    DateOnly? EffectiveDate = null);

public sealed record SalaryProfileDto(
    long Id,
    long EmployeeId,
    decimal BaseSalary,
    decimal PositionAllowance,
    decimal OtherAllowance,
    decimal? InsuranceSalary,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate,
    bool IsCurrent,
    string? Note);

public static class EducationLevelCatalog
{
    public const string Master = "THAC_SI";
    public const string Bachelor = "DAI_HOC";
    public const string College = "CAO_DANG";
    public const string Intermediate = "TRUNG_CAP";
    public const string Other = "KHAC";

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Bachelor;
        var trimmed = input.Trim().ToUpperInvariant();
        if (trimmed is "THAC_SI" or "THẠC SĨ" or "THAC SI" or "MASTER") return Master;
        if (trimmed is "DAI_HOC" or "ĐẠI HỌC" or "ĐH" or "DAI HOC" or "BACHELOR") return Bachelor;
        if (trimmed is "CAO_DANG" or "CAO ĐẲNG" or "CĐ" or "CAO DANG" or "COLLEGE") return College;
        if (trimmed is "TRUNG_CAP" or "TRUNG CẤP" or "TC" or "TRUNG CAP") return Intermediate;
        return Other;
    }

    public static string ToDisplayName(string? code)
    {
        return Normalize(code) switch
        {
            Master => "Thạc sĩ",
            Bachelor => "Đại học",
            College => "Cao đẳng",
            Intermediate => "Trung cấp",
            _ => "Khác"
        };
    }
}

public sealed record EmployeeTimelineEventDto(
    long Id,
    long EmployeeId,
    string EventType,
    string Title,
    string Description,
    DateTimeOffset Timestamp,
    long? CreatedByUserId,
    string? CreatedByName);

public static class GenderCatalog
{
    public const string Male = "NAM";
    public const string Female = "NU";
    public const string Other = "KHAC";

    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var trimmed = input.Trim().ToUpperInvariant();
        if (trimmed is "NAM" or "MALE") return Male;
        if (trimmed is "NU" or "NỮ" or "FEMALE") return Female;
        if (trimmed is "KHAC" or "KHÁC" or "OTHER") return Other;
        return Other;
    }

    public static string ToDisplayName(string? code)
    {
        return Normalize(code) switch
        {
            Male => "Nam",
            Female => "Nữ",
            Other => "Khác",
            _ => "Chưa cập nhật"
        };
    }
}
