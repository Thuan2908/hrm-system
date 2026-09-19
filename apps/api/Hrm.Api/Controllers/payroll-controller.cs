using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Payroll.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/payroll")]
[Authorize]
public sealed class PayrollController(IPayrollService payrollService) : ControllerBase
{
    [HttpGet("my-payslips")]
    [Authorize(Policy = PermissionCodes.PayrollSelfRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayslipSummaryDto>>>> GetMyPayslips(CancellationToken cancellationToken)
    {
        var result = await payrollService.GetMyPayslipsAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("my-payslips/{id:long}")]
    [Authorize(Policy = PermissionCodes.PayrollSelfRead)]
    public async Task<ActionResult<ApiResponse<PayslipDetailDto>>> GetPayslipDetail(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await payrollService.GetPayslipDetailAsync(GetUserId(), id, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing in security context.");
}
