using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Leave.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/leave")]
[Authorize]
public sealed class LeaveController(ILeaveService leaveService) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<LeaveBalanceSummaryDto>>> GetBalance(CancellationToken cancellationToken)
    {
        var result = await leaveService.GetLeaveBalanceAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("my-requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveRequestDto>>>> GetMyRequests(CancellationToken cancellationToken)
    {
        var result = await leaveService.GetMyRequestsAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("request")]
    [Authorize(Policy = PermissionCodes.LeaveSelfCreate)]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> CreateRequest(
        [FromBody] CreateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await leaveService.CreateLeaveRequestAsync(GetUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("cancel/{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> CancelRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        await leaveService.CancelLeaveRequestAsync(GetUserId(), id, cancellationToken);
        return Ok(ApiResponse.Ok("Đã hủy đơn xin nghỉ phép thành công."));
    }

    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing in security context.");
}
