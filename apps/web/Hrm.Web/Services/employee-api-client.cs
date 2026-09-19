using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;
using Hrm.Web.Authorization;

namespace Hrm.Web.Services;

public sealed class EmployeeApiClient(HttpClient httpClient, BrowserSessionStore sessionStore)
{
    public async Task<ApiResponse<PagedResult<EmployeeDto>>?> SearchEmployeesAsync(
        string? keyword = null,
        long? departmentId = null,
        long? positionId = null,
        string? status = null,
        string sortBy = "createdAt",
        bool descending = true,
        int page = 1,
        int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"sortBy={Uri.EscapeDataString(sortBy)}",
            $"descending={descending.ToString().ToLowerInvariant()}"
        };

        if (!string.IsNullOrWhiteSpace(keyword)) query.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        if (departmentId.HasValue && departmentId.Value > 0) query.Add($"departmentId={departmentId.Value}");
        if (positionId.HasValue && positionId.Value > 0) query.Add($"positionId={positionId.Value}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status.Trim())}");

        var url = $"api/v1/employees?{string.Join("&", query)}";
        return await SendAsync<ApiResponse<PagedResult<EmployeeDto>>>(HttpMethod.Get, url, null, cancellationToken);
    }

    public async Task<ApiResponse<EmployeeDto>?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<EmployeeDto>>(HttpMethod.Get, $"api/v1/employees/{id}", null, cancellationToken);

    public async Task<ApiResponse<IReadOnlyCollection<DepartmentDto>>?> GetDepartmentsAsync(CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<IReadOnlyCollection<DepartmentDto>>>(HttpMethod.Get, "api/v1/departments", null, cancellationToken);

    public async Task<ApiResponse<IReadOnlyCollection<PositionDto>>?> GetPositionsAsync(CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<IReadOnlyCollection<PositionDto>>>(HttpMethod.Get, "api/v1/positions", null, cancellationToken);

    public async Task<ApiResponse<EmployeeDto>?> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<EmployeeDto>>(HttpMethod.Post, "api/v1/employees", request, cancellationToken);

    public async Task<ApiResponse<EmployeeDto>?> UpdateEmployeeAsync(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<EmployeeDto>>(HttpMethod.Put, $"api/v1/employees/{id}", request, cancellationToken);

    public async Task<ApiResponse<object>?> TransferEmployeeAsync(long id, TransferEmployeeRequest request, CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<object>>(HttpMethod.Post, $"api/v1/employees/{id}/transfer", request, cancellationToken);

    public async Task<ApiResponse<object>?> PromoteEmployeeAsync(long id, PromoteEmployeeRequest request, CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<object>>(HttpMethod.Post, $"api/v1/employees/{id}/promote", request, cancellationToken);

    public async Task<ApiResponse<IReadOnlyCollection<EmployeeTimelineEventDto>>?> GetEmployeeTimelineAsync(long id, CancellationToken cancellationToken = default) =>
        await SendAsync<ApiResponse<IReadOnlyCollection<EmployeeTimelineEventDto>>>(HttpMethod.Get, $"api/v1/employees/{id}/timeline", null, cancellationToken);

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var session = await sessionStore.GetAsync();
        if (session is null) return default;

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var fail = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: cancellationToken);
            var error = fail?.Error ?? new ApiError("HTTP_ERROR", $"Request failed with status code {(int)response.StatusCode}", Array.Empty<ApiErrorDetail>());

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                var dataType = typeof(T).GetGenericArguments()[0];
                var failMethod = typeof(ApiResponse).GetMethod(nameof(ApiResponse.Fail))!.MakeGenericMethod(dataType);
                return (T?)failMethod.Invoke(null, [error]);
            }
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }
}
