using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Auth.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, GetIpAddress(), GetUserAgent(), cancellationToken);
        return Ok(ApiResponse.Ok(result, new ApiMeta(TraceId: HttpContext.TraceIdentifier)));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request, GetIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok(result, new ApiMeta(TraceId: HttpContext.TraceIdentifier)));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, GetIpAddress(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [Authorize]
    [HttpPost("heartbeat")]
    public async Task<ActionResult<ApiResponse<object>>> Heartbeat(
        [FromBody] HeartbeatRequest? request,
        CancellationToken cancellationToken)
    {
        await authService.UpdateHeartbeatAsync(GetUserId(), request?.DeviceId, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { status = "ok" }));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserSessionDto>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await authService.GetSessionAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> GetProfile(CancellationToken cancellationToken)
    {
        var result = await authService.GetEmployeeProfileAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [Authorize]
    [HttpGet("session-status")]
    public async Task<ActionResult<ApiResponse<AccountStatusDto>>> GetSessionStatus(CancellationToken cancellationToken)
    {
        var result = await authService.CheckAccountStatusAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserSessionDto>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.UpdateProfileAsync(GetUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(GetUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { message = "Đổi mật khẩu thành công." }));
    }

    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing.");

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => HttpContext.Request.Headers.UserAgent.ToString();
}
