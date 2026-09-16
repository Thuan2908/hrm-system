using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Auth.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "ADMIN")]
public sealed class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("users")]
    [Authorize(Policy = PermissionCodes.AdminUserSearch)]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> SearchUsers(
        [FromQuery] string? keyword,
        [FromQuery] string? department,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool descending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminService.SearchUsersAsync(
            keyword, department, role, status, sortBy, descending, page, pageSize, cancellationToken);
        return Ok(ApiResponse.Ok(result, new ApiMeta(page, pageSize, result.Total, result.TotalPages)));
    }

    [HttpPost("users")]
    [Authorize(Policy = PermissionCodes.AdminUserManage)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> CreateUser(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.CreateUserAsync(request, GetActorId(), cancellationToken);
        return Created($"api/v1/admin/users/{result.Id}", ApiResponse.Ok(result));
    }

    [HttpGet("employees")]
    [Authorize(Policy = PermissionCodes.AdminUserRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeAccountOptionDto>>>> GetEmployees(
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetEmployeeOptionsAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("users/{userId:long}")]
    [Authorize(Policy = PermissionCodes.AdminUserManage)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateUser(
        long userId,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.UpdateUserAsync(userId, request, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("users/{userId:long}/status")]
    [Authorize(Policy = PermissionCodes.AdminUserManage)]
    public async Task<ActionResult<ApiResponse<object>>> SetStatus(
        long userId,
        SetAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        await adminService.SetStatusAsync(userId, request.IsActive, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [HttpPut("users/{userId:long}/lock")]
    [Authorize(Policy = PermissionCodes.AdminUserManage)]
    public async Task<ActionResult<ApiResponse<object>>> SetLock(
        long userId,
        SetAccountLockRequest request,
        CancellationToken cancellationToken)
    {
        await adminService.SetLockAsync(userId, request.IsLocked, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [HttpPut("users/{userId:long}/roles")]
    [Authorize(Policy = PermissionCodes.AdminRbacManage)]
    public async Task<ActionResult<ApiResponse<object>>> SetRoles(
        long userId,
        SetUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        await adminService.SetRolesAsync(userId, request.Roles, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [HttpPut("users/{userId:long}/password")]
    [Authorize(Policy = PermissionCodes.AdminUserManage)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        long userId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await adminService.ResetPasswordAsync(userId, request.NewPassword, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [HttpGet("roles")]
    [Authorize(Policy = PermissionCodes.AdminUserRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleDto>>>> GetRoles(CancellationToken cancellationToken)
    {
        var result = await adminService.GetRolesAsync(cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("roles/{roleId:long}/permissions")]
    [Authorize(Policy = PermissionCodes.AdminRbacManage)]
    public async Task<ActionResult<ApiResponse<object>>> SetRolePermissions(
        long roleId,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        await adminService.SetRolePermissionsAsync(roleId, request.Permissions, GetActorId(), cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }));
    }

    [HttpGet("audit")]
    [Authorize(Policy = PermissionCodes.AuditRead)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> SearchAudit(
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminService.SearchAuditAsync(action, page, pageSize, cancellationToken);
        return Ok(ApiResponse.Ok(result, new ApiMeta(page, pageSize, result.Total, result.TotalPages)));
    }

    private long GetActorId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing.");
}
