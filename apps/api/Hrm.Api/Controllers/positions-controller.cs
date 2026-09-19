using Hrm.Contracts;
using Hrm.Modules.Employees.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/positions")]
[Authorize]
public sealed class PositionsController(IEmployeeManagementService employeeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.EmployeeRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PositionDto>>>> GetPositions(
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetPositionsAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}
