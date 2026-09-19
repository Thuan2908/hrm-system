using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Employees.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public sealed class EmployeesController(
    IEmployeeManagementService employeeService,
    IEmployeeOffboardingService offboardingService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.EmployeeRead)]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> Search(
        [FromQuery] string? keyword,
        [FromQuery] long? departmentId,
        [FromQuery] long? positionId,
        [FromQuery] string? status,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool descending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var result = await employeeService.SearchEmployeesAsync(
            keyword, departmentId, positionId, status, sortBy, descending, page, pageSize, cancellationToken);

        return Ok(ApiResponse.Ok(result, new ApiMeta(page, pageSize, result.Total, result.TotalPages)));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = PermissionCodes.EmployeeRead)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetEmployeeByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse.Fail<EmployeeDto>(new ApiError("EMPLOYEE_NOT_FOUND", $"Không tìm thấy nhân sự có ID {id}.", [])));
        }

        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.EmployeeWrite)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.CreateEmployeeAsync(request, GetActorId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = PermissionCodes.EmployeeWrite)]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Update(
        long id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.UpdateEmployeeAsync(id, request, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{id:long}/transfer")]
    [Authorize(Policy = PermissionCodes.EmployeeTransfer)]
    public async Task<ActionResult<ApiResponse<object>>> Transfer(
        long id,
        [FromBody] TransferEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        await employeeService.TransferEmployeeAsync(id, request, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [HttpPost("{id:long}/promote")]
    [Authorize(Policy = PermissionCodes.EmployeeWrite)]
    public async Task<ActionResult<ApiResponse<object>>> Promote(
        long id,
        [FromBody] PromoteEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        await employeeService.PromoteEmployeeAsync(id, request, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [HttpGet("{id:long}/timeline")]
    [Authorize(Policy = PermissionCodes.EmployeeRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeTimelineEventDto>>>> GetTimeline(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetEmployeeTimelineAsync(id, cancellationToken);
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
