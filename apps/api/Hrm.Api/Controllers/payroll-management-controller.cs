using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Payroll.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/payroll-management")]
[Authorize]
public sealed class PayrollManagementController(IPayrollService payrollService) : ControllerBase
{
    [HttpGet("all")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayrollManagementItemDto>>>> GetAllPayslips(
        [FromQuery] short? month,
        [FromQuery] short? year,
        CancellationToken cancellationToken)
    {
        var hasAccess = User.IsInRole("ADMIN")
            || User.HasClaim("permission", PermissionCodes.PayrollRun)
            || User.HasClaim("permission", PermissionCodes.PayrollFinalize)
            || User.HasClaim("permission", "PAYROLL_MANAGE");

        if (!hasAccess) return Forbid();

        var result = await payrollService.GetAllPayslipsAsync(month, year, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<int>>> CalculatePayroll(
        [FromBody] CalculatePayrollRequest request,
        CancellationToken cancellationToken)
    {
        var hasAccess = User.IsInRole("ADMIN")
            || User.HasClaim("permission", PermissionCodes.PayrollRun)
            || User.HasClaim("permission", "PAYROLL_MANAGE");

        if (!hasAccess) return Forbid();

        var count = await payrollService.CalculatePeriodPayrollAsync(request.MonthPeriod, request.YearPeriod, GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(count));
    }

    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing in security context.");
}
