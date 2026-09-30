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

    [HttpPut("requests/{id:guid}")]
    [Authorize(Policy = PermissionCodes.LeaveSelfCreate)]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> UpdateRequest(
        Guid id,
        [FromBody] UpdateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await leaveService.UpdateLeaveRequestAsync(GetUserId(), id, request, cancellationToken);
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

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingLeaveApprovalDto>>>> GetPending(CancellationToken cancellationToken)
    {
        var hasAccess = User.IsInRole("ADMIN")
            || User.HasClaim("permission", PermissionCodes.LeaveTeamApprove)
            || User.HasClaim("permission", PermissionCodes.LeaveHrApprove)
            || User.HasClaim("permission", "LEAVE_APPROVE")
            || User.HasClaim("permission", "LEAVE_HR_APPROVE");

        if (!hasAccess) return Forbid();

        var result = await leaveService.GetPendingRequestsAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> ApproveRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        var hasAccess = User.IsInRole("ADMIN")
            || User.HasClaim("permission", PermissionCodes.LeaveTeamApprove)
            || User.HasClaim("permission", PermissionCodes.LeaveHrApprove)
            || User.HasClaim("permission", "LEAVE_APPROVE")
            || User.HasClaim("permission", "LEAVE_HR_APPROVE");

        if (!hasAccess) return Forbid();

        await leaveService.ApproveLeaveRequestAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok("Đã phê duyệt đơn xin nghỉ phép thành công."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> RejectRequest(
        Guid id,
        [FromBody] RejectLeaveRequestInput? input,
        CancellationToken cancellationToken)
    {
        var hasAccess = User.IsInRole("ADMIN")
            || User.HasClaim("permission", PermissionCodes.LeaveTeamApprove)
            || User.HasClaim("permission", PermissionCodes.LeaveHrApprove)
            || User.HasClaim("permission", "LEAVE_APPROVE")
            || User.HasClaim("permission", "LEAVE_HR_APPROVE");

        if (!hasAccess) return Forbid();

        await leaveService.RejectLeaveRequestAsync(id, input?.Reason, GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok("Đã từ chối đơn xin nghỉ phép."));
    }

    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing in security context.");
}
