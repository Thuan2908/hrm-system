using Hrm.Contracts;
using Hrm.Modules.Employees.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize]
public sealed class DepartmentsController(IEmployeeManagementService employeeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.EmployeeRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DepartmentDto>>>> GetDepartments(
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetDepartmentsAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }
}
