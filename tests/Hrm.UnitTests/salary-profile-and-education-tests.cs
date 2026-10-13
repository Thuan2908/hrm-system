using Hrm.Contracts;

namespace Hrm.UnitTests;

public sealed class SalaryProfileAndEducationTests
{
    [Theory]
    [InlineData("Đại học", EducationLevelCatalog.Bachelor)]
    [InlineData("ĐH", EducationLevelCatalog.Bachelor)]
    [InlineData("DAI_HOC", EducationLevelCatalog.Bachelor)]
    [InlineData("Bachelor", EducationLevelCatalog.Bachelor)]
    [InlineData("Thạc sĩ", EducationLevelCatalog.Master)]
    [InlineData("THAC_SI", EducationLevelCatalog.Master)]
    [InlineData("Cao đẳng", EducationLevelCatalog.College)]
    [InlineData("CĐ", EducationLevelCatalog.College)]
    [InlineData("Trung cấp", EducationLevelCatalog.Intermediate)]
    [InlineData("Khác", EducationLevelCatalog.Other)]
    [InlineData(null, EducationLevelCatalog.Bachelor)]
    public void EducationLevelCatalogNormalizesCorrectly(string? input, string expected)
    {
        var result = EducationLevelCatalog.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(EducationLevelCatalog.Bachelor, "Đại học")]
    [InlineData(EducationLevelCatalog.Master, "Thạc sĩ")]
    [InlineData(EducationLevelCatalog.College, "Cao đẳng")]
    [InlineData(EducationLevelCatalog.Intermediate, "Trung cấp")]
    [InlineData(EducationLevelCatalog.Other, "Khác")]
    public void EducationLevelCatalogToDisplayNameReturnsVietnameseName(string code, string expectedName)
    {
        var result = EducationLevelCatalog.ToDisplayName(code);
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void SalaryProfileDtoInitializesProperly()
    {
        var profile = new SalaryProfileDto(
            Id: 1,
            EmployeeId: 10,
            BaseSalary: 15_000_000m,
            PositionAllowance: 2_000_000m,
            OtherAllowance: 1_000_000m,
            InsuranceSalary: 15_000_000m,
            EffectiveDate: new DateOnly(2026, 1, 1),
            ExpiryDate: null,
            IsCurrent: true,
            Note: "Hợp đồng lao động chính thức");

        Assert.Equal(1, profile.Id);
        Assert.Equal(10, profile.EmployeeId);
        Assert.Equal(15_000_000m, profile.BaseSalary);
        Assert.Equal(2_000_000m, profile.PositionAllowance);
        Assert.True(profile.IsCurrent);
        Assert.Null(profile.ExpiryDate);
    }

    [Fact]
    public void PayslipSummaryDtoIncludesSnapshotFields()
    {
        var payslip = new PayslipSummaryDto(
            Id: 101,
            EmployeeId: 5,
            MonthPeriod: 8,
            YearPeriod: 2026,
            ActualDays: 22m,
            GrossSalary: 18_000_000m,
            BhxhDeduct: 1_890_000m,
            TaxDeduct: 255_500m,
            NetSalary: 15_854_500m,
            StandardWorkingDays: 22m,
            SnapshotBaseSalary: 15_000_000m,
            SnapshotPositionAllowance: 2_000_000m,
            SnapshotOtherAllowance: 1_000_000m);

        Assert.Equal(22m, payslip.StandardWorkingDays);
        Assert.Equal(15_000_000m, payslip.SnapshotBaseSalary);
        Assert.Equal(2_000_000m, payslip.SnapshotPositionAllowance);
        Assert.Equal(1_000_000m, payslip.SnapshotOtherAllowance);
    }
}




