using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Employees.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public sealed class EmployeesController(IEmployeeOffboardingService offboardingService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.EmployeeRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeAccountOptionDto>>>> GetDirectory(
        [FromServices] Hrm.Modules.Auth.Application.IAdminService adminService,
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetEmployeeOptionsAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{employeeId:long}/offboard")]
    [Authorize(Policy = PermissionCodes.EmployeeOffboard)]
    public async Task<ActionResult<ApiResponse<object>>> Offboard(
        long employeeId,
        CancellationToken cancellationToken)
    {
        await offboardingService.OffboardAsync(employeeId, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    private long GetActorId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing.");
}
