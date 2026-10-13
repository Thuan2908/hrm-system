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
    string Name);

public sealed record PositionDto(
    long Id,
    string? Code,
    string Name);

public sealed record TransferDepartmentRequest(
    long TargetDepartmentId,
    long? TargetPositionId = null,
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
