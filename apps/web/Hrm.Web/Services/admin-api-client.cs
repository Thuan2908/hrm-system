using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public sealed class AdminApiClient(HttpClient httpClient, IAuthApiClient authApiClient)
{
    public Task<ApiResponse<PagedResult<AdminUserDto>>?> SearchUsersAsync(
        string? keyword,
        string? department,
        string? role,
        string? status,
        string sortBy,
        bool descending,
        int page,
        CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<AdminUserDto>>(
            HttpMethod.Get,
            $"api/v1/admin/users?keyword={Uri.EscapeDataString(keyword ?? string.Empty)}&department={Uri.EscapeDataString(department ?? string.Empty)}&role={Uri.EscapeDataString(role ?? string.Empty)}&status={Uri.EscapeDataString(status ?? string.Empty)}&sortBy={Uri.EscapeDataString(sortBy)}&descending={descending}&page={page}&pageSize=20",
            null,
            cancellationToken);

    public Task<ApiResponse<AdminUserDto>?> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<AdminUserDto>(HttpMethod.Post, "api/v1/admin/users", request, cancellationToken);

    public Task<ApiResponse<IReadOnlyCollection<EmployeeAccountOptionDto>>?> GetEmployeesAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyCollection<EmployeeAccountOptionDto>>(
            HttpMethod.Get, "api/v1/admin/employees", null, cancellationToken);

    public Task<ApiResponse<object>?> SetStatusAsync(
        long userId,
        bool isActive,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Put, $"api/v1/admin/users/{userId}/status", new SetAccountStatusRequest(isActive), cancellationToken);

    public Task<ApiResponse<object>?> SetLockAsync(
        long userId,
        bool isLocked,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Put, $"api/v1/admin/users/{userId}/lock", new SetAccountLockRequest(isLocked), cancellationToken);

    public Task<ApiResponse<object>?> ResetPasswordAsync(
        long userId,
        string newPassword,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Put, $"api/v1/admin/users/{userId}/password", new ResetUserPasswordRequest(newPassword), cancellationToken);

    public Task<ApiResponse<object>?> SetRolesAsync(
        long userId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Put, $"api/v1/admin/users/{userId}/roles", new SetUserRolesRequest(roles), cancellationToken);

    public Task<ApiResponse<object>?> OffboardEmployeeAsync(
        long employeeId,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Post, $"api/v1/employees/{employeeId}/offboard", null, cancellationToken);

    public Task<ApiResponse<IReadOnlyCollection<RoleDto>>?> GetRolesAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyCollection<RoleDto>>(HttpMethod.Get, "api/v1/admin/roles", null, cancellationToken);

    public Task<ApiResponse<RoleDto>?> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<RoleDto>(HttpMethod.Post, "api/v1/admin/roles", request, cancellationToken);

    public Task<ApiResponse<object>?> DeleteRoleAsync(
        long roleId,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Delete, $"api/v1/admin/roles/{roleId}", null, cancellationToken);

    public Task<ApiResponse<object>?> SetRolePermissionsAsync(
        long roleId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Put, $"api/v1/admin/roles/{roleId}/permissions", new SetRolePermissionsRequest(permissions), cancellationToken);

    public Task<ApiResponse<PagedResult<AuditLogDto>>?> SearchAuditAsync(
        string? action,
        int page,
        CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<AuditLogDto>>(
            HttpMethod.Get,
            $"api/v1/admin/audit?action={Uri.EscapeDataString(action ?? string.Empty)}&page={page}&pageSize=20",
            null,
            cancellationToken);

    private async Task<ApiResponse<T>?> SendAsync<T>(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var token = await authApiClient.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }
}
