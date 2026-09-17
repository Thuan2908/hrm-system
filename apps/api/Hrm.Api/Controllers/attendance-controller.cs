using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Attendance.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public sealed class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<AttendanceTodayResponse>>> GetTodayStatus(CancellationToken cancellationToken)
    {
        var result = await attendanceService.GetTodayStatusAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("check-in")]
    [Authorize(Policy = PermissionCodes.AttendanceSelfWrite)]
    public async Task<ActionResult<ApiResponse<CheckInResultDto>>> CheckIn(
        [FromBody] CheckInRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await attendanceService.CheckInAsync(
            GetUserId(), request ?? new CheckInRequest(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("check-out")]
    [Authorize(Policy = PermissionCodes.AttendanceSelfWrite)]
    public async Task<ActionResult<ApiResponse<CheckOutResultDto>>> CheckOut(
        [FromBody] CheckOutRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await attendanceService.CheckOutAsync(
            GetUserId(), request ?? new CheckOutRequest(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AttendanceRecordDto>>>> GetHistory(
        [FromQuery] int days = 14,
        CancellationToken cancellationToken = default)
    {
        var result = await attendanceService.GetMyHistoryAsync(GetUserId(), days, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing in security context.");
}
