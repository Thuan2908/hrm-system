using Hrm.Contracts;

namespace Hrm.UnitTests;

public sealed class ReportContractTests
{
    [Fact]
    public void HrDashboardSummaryDtoInitializesWithCorrectValues()
    {
        var summary = new HrDashboardSummaryDto(
            100,
            90,
            10,
            5,
            150_000_000m,
            3,
            85);

        Assert.Equal(100, summary.TotalEmployees);
        Assert.Equal(90, summary.ActiveEmployees);
        Assert.Equal(10, summary.ResignedEmployees);
        Assert.Equal(5, summary.TotalDepartments);
        Assert.Equal(150_000_000m, summary.TotalMonthlyPayroll);
        Assert.Equal(3, summary.PendingLeaveRequests);
        Assert.Equal(85, summary.TodayAttendanceCount);
    }

    [Fact]
    public void HeadcountReportDtoInitializesWithCorrectCollections()
    {
        var departments = new List<DepartmentDistributionDto>
        {
            new("Kỹ thuật", 40, 40m),
            new("Kinh doanh", 60, 60m)
        };

        var educations = new List<EducationDistributionDto>
        {
            new("Đại học", 80, 80m),
            new("Cao đẳng", 20, 20m)
        };

        var report = new HeadcountReportDto(
            100,
            95,
            2,
            3,
            departments,
            educations);

        Assert.Equal(100, report.TotalEmployees);
        Assert.Equal(95, report.ActiveCount);
        Assert.Equal(2, report.OnLeaveCount);
        Assert.Equal(3, report.ResignedCount);
        Assert.Equal(2, report.Departments.Count);
        Assert.Equal(2, report.EducationLevels.Count);
    }
}
