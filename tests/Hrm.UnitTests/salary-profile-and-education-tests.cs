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

    [Theory]
    [InlineData("NAM", GenderCatalog.Male)]
    [InlineData("Nam", GenderCatalog.Male)]
    [InlineData("MALE", GenderCatalog.Male)]
    [InlineData("Male", GenderCatalog.Male)]
    [InlineData("NU", GenderCatalog.Female)]
    [InlineData("Nữ", GenderCatalog.Female)]
    [InlineData("NỮ", GenderCatalog.Female)]
    [InlineData("FEMALE", GenderCatalog.Female)]
    [InlineData("Female", GenderCatalog.Female)]
    [InlineData("KHAC", GenderCatalog.Other)]
    [InlineData("Khác", GenderCatalog.Other)]
    [InlineData("OTHER", GenderCatalog.Other)]
    [InlineData("Other", GenderCatalog.Other)]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void GenderCatalogNormalizesCorrectly(string? input, string? expected)
    {
        var result = GenderCatalog.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(GenderCatalog.Male, "Nam")]
    [InlineData(GenderCatalog.Female, "Nữ")]
    [InlineData(GenderCatalog.Other, "Khác")]
    [InlineData(null, "Chưa cập nhật")]
    [InlineData("", "Chưa cập nhật")]
    public void GenderCatalogToDisplayNameReturnsProperName(string? code, string expected)
    {
        var result = GenderCatalog.ToDisplayName(code);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DepartmentPositionPolicyIsCompatibleWithForeignKeyChecksAccurately()
    {
        // 1. When positionDepartmentId matches targetDepartmentId, returns true
        Assert.True(DepartmentPositionPolicy.IsCompatible(10, 10, "HR", "Phòng Nhân sự", "Kỹ sư", "ENG"));

        // 2. When positionDepartmentId does not match targetDepartmentId, returns false
        Assert.False(DepartmentPositionPolicy.IsCompatible(10, 20, "HR", "Phòng Nhân sự", "Chuyên viên nhân sự", "HR_SPEC"));

        // 3. When positionDepartmentId is null or 0 (General/Cross-departmental position), returns true for any department
        Assert.True(DepartmentPositionPolicy.IsCompatible(10, null, "HR", "Phòng Nhân sự", "Chuyên viên tuyển dụng", "HR_REC"));
        Assert.True(DepartmentPositionPolicy.IsCompatible(10, null, "HR", "Phòng Nhân sự", "Thực tập sinh", "INTERN"));
        Assert.True(DepartmentPositionPolicy.IsCompatible(10, 0, "HR", "Phòng Nhân sự", "Tổng Giám Đốc", "CEO"));
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

    [Theory]
    [InlineData("Chủ Tịch Hội Đồng Quản Trị", 11)]
    [InlineData("Tổng Giám Đốc", 10)]
    [InlineData("Giám Đốc Điều Hành", 9)]
    [InlineData("Director of Engineering", 9)]
    [InlineData("Phó Giám Đốc", 8)]
    [InlineData("Trưởng Phòng Kỹ Thuật", 7)]
    [InlineData("Head of Marketing", 7)]
    [InlineData("Lead Manager", 7)]
    [InlineData("Phó Phòng Kế Toán", 6)]
    [InlineData("Trưởng Nhóm Phát Triển", 5)]
    [InlineData("Team Leader", 5)]
    [InlineData("Senior Software Engineer", 4)]
    [InlineData("Chuyên Viên Kinh Doanh", 3)]
    [InlineData("Kỹ Sư Phần Mềm", 3)]
    [InlineData("Nhân Viên Bán Hàng", 2)]
    [InlineData("Thực tập sinh", 1)]
    [InlineData(null, 1)]
    public void PositionHierarchyPolicyMapsKeywordsToRanks(string? title, int expectedRank)
    {
        var rank = PositionHierarchyPolicy.GetPositionRank(title);
        Assert.Equal(expectedRank, rank);
    }

    [Fact]
    public void PositionHierarchyPolicyPrioritizesDbLevelOverKeywords()
    {
        // Khi CSDL có positionLevel, hệ thống phải ưu tiên giá trị CSDL thay vì keyword
        var rank1 = PositionHierarchyPolicy.GetPositionRank("Nhân viên thử việc", positionLevel: 4);
        Assert.Equal(4, rank1);

        var rank2 = PositionHierarchyPolicy.GetPositionRank("Giám đốc", positionLevel: 2);
        Assert.Equal(2, rank2);

        // Hỗ trợ mở rộng cấp bậc linh hoạt (không bị chặn trần cố định = 5)
        var rank3 = PositionHierarchyPolicy.GetPositionRank("Chuyên viên", positionLevel: 10);
        Assert.Equal(10, rank3);

        var rank4 = PositionHierarchyPolicy.GetPositionRank("Chuyên viên", positionLevel: -1);
        // Fallback to keyword when <= 0 (Chuyên viên = cấp 3)
        Assert.Equal(3, rank4);
    }

    [Fact]
    public void PositionHierarchyPolicyValidatesPromotionCorrectly()
    {
        // 1. Giám đốc / Lead Manager (Rank 5) nâng Staff (1) lên Manager (4) cùng phòng -> Hợp lệ
        var ok1 = PositionHierarchyPolicy.ValidatePromotion(
            actorRank: 5,
            isActorAdminOrBoard: false,
            actorDepartmentId: 10,
            targetDepartmentId: 10,
            currentTargetRank: 1,
            newTargetRank: 4,
            out var err1);
        Assert.True(ok1);
        Assert.Null(err1);

        // 2. Manager (Rank 4) cố gắng nâng Staff (1) lên Manager (4) -> Bị chặn vì người nâng không cao hơn vị trí mới
        var ok2 = PositionHierarchyPolicy.ValidatePromotion(
            actorRank: 4,
            isActorAdminOrBoard: false,
            actorDepartmentId: 10,
            targetDepartmentId: 10,
            currentTargetRank: 1,
            newTargetRank: 4,
            out var err2);
        Assert.False(ok2);
        Assert.NotNull(err2);

        // 3. Manager (Rank 4) nâng Staff (1) lên Team Leader (3) cùng phòng -> Hợp lệ
        var ok3 = PositionHierarchyPolicy.ValidatePromotion(
            actorRank: 4,
            isActorAdminOrBoard: false,
            actorDepartmentId: 10,
            targetDepartmentId: 10,
            currentTargetRank: 1,
            newTargetRank: 3,
            out var err3);
        Assert.True(ok3);
        Assert.Null(err3);

        // 4. Khác phòng ban (Actor phòng 10, Target phòng 20, không phải Admin/BGD) -> Bị chặn
        var ok4 = PositionHierarchyPolicy.ValidatePromotion(
            actorRank: 5,
            isActorAdminOrBoard: false,
            actorDepartmentId: 10,
            targetDepartmentId: 20,
            currentTargetRank: 1,
            newTargetRank: 2,
            out var err4);
        Assert.False(ok4);
        Assert.Contains("phạm vi phòng ban", err4);

        // 5. Admin / Ban Giám Đốc -> Được phép nâng xuyên phòng ban
        var ok5 = PositionHierarchyPolicy.ValidatePromotion(
            actorRank: 5,
            isActorAdminOrBoard: true,
            actorDepartmentId: 99,
            targetDepartmentId: 20,
            currentTargetRank: 1,
            newTargetRank: 4,
            out var err5);
        Assert.True(ok5);
        Assert.Null(err5);

        // 6. Vị trí mới không cao hơn vị trí hiện tại (ngang cấp hoặc hạ cấp) -> Bị chặn
        var ok6 = PositionHierarchyPolicy.ValidatePromotion(
            actorRank: 5,
            isActorAdminOrBoard: true,
            actorDepartmentId: 10,
            targetDepartmentId: 10,
            currentTargetRank: 3,
            newTargetRank: 2,
            out var err6);
        Assert.False(ok6);
        Assert.Contains("cao hơn", err6);
    }
}

