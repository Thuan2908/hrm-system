using Hrm.Contracts;
using Hrm.Modules.Reports.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Policy = PermissionCodes.ReportRead)]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<HrDashboardSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await reportService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("headcount")]
    public async Task<ActionResult<ApiResponse<HeadcountReportDto>>> GetHeadcount(CancellationToken cancellationToken)
    {
        var result = await reportService.GetHeadcountReportAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("tenure")]
    public async Task<ActionResult<ApiResponse<TenureReportDto>>> GetTenure(CancellationToken cancellationToken)
    {
        var result = await reportService.GetTenureReportAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<ApiResponse<AttendanceReportDto>>> GetAttendance(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await reportService.GetAttendanceReportAsync(month, year, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("payroll")]
    public async Task<ActionResult<ApiResponse<PayrollReportDto>>> GetPayroll(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await reportService.GetPayrollReportAsync(month, year, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}
