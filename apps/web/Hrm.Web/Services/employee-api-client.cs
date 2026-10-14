using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface IEmployeeApiClient
{
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(
        string? keyword = null,
        long? departmentId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<ApiResponse<EmployeeDto>?> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<EmployeeDto>?> UpdateEmployeeAsync(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<EmployeeDto>?> TransferEmployeeAsync(long employeeId, TransferDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<EmployeeDto>?> PromoteEmployeeAsync(long employeeId, PromoteEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<bool> OffboardEmployeeAsync(long employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentDto>?> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentDto>?> UpdateDepartmentAsync(long id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionDto>?> CreatePositionAsync(CreatePositionRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionDto>?> UpdatePositionAsync(long id, UpdatePositionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalaryProfileDto>> GetSalaryHistoryAsync(long employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeTimelineEventDto>> GetEmployeeTimelineAsync(long employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default);
}

public sealed class EmployeeApiClient(HttpClient httpClient) : IEmployeeApiClient
{
    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(
        string? keyword = null,
        long? departmentId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword)) queryParams.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        if (departmentId.HasValue && departmentId.Value > 0) queryParams.Add($"departmentId={departmentId.Value}");
        if (!string.IsNullOrWhiteSpace(status)) queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

        var url = "api/v1/employees" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
        var response = await SendAsync<IReadOnlyList<EmployeeDto>>(HttpMethod.Get, url, null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<EmployeeDto>(HttpMethod.Get, $"api/v1/employees/{id}", null, cancellationToken);
        return response?.Data;
    }

    public async Task<ApiResponse<EmployeeDto>?> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<EmployeeDto>(HttpMethod.Post, "api/v1/employees", request, cancellationToken);
    }

    public async Task<ApiResponse<EmployeeDto>?> UpdateEmployeeAsync(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<EmployeeDto>(HttpMethod.Put, $"api/v1/employees/{id}", request, cancellationToken);
    }

    public async Task<ApiResponse<EmployeeDto>?> TransferEmployeeAsync(long employeeId, TransferDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<EmployeeDto>(HttpMethod.Post, $"api/v1/employees/{employeeId}/transfer", request, cancellationToken);
    }

    public async Task<ApiResponse<EmployeeDto>?> PromoteEmployeeAsync(long employeeId, PromoteEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<EmployeeDto>(HttpMethod.Post, $"api/v1/employees/{employeeId}/promote", request, cancellationToken);
    }

    public async Task<bool> OffboardEmployeeAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<object>(HttpMethod.Post, $"api/v1/employees/{employeeId}/offboard", null, cancellationToken);
        if (response is null || !response.Success)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Không thể thực hiện thôi việc cho nhân sự.");
        }
        return true;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var url = includeInactive ? "api/v1/employees/departments?includeInactive=true" : "api/v1/employees/departments";
        var response = await SendAsync<IReadOnlyList<DepartmentDto>>(HttpMethod.Get, url, null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<ApiResponse<DepartmentDto>?> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<DepartmentDto>(HttpMethod.Post, "api/v1/employees/departments", request, cancellationToken);
    }

    public async Task<ApiResponse<DepartmentDto>?> UpdateDepartmentAsync(long id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<DepartmentDto>(HttpMethod.Put, $"api/v1/employees/departments/{id}", request, cancellationToken);
    }

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var url = includeInactive ? "api/v1/employees/positions?includeInactive=true" : "api/v1/employees/positions";
        var response = await SendAsync<IReadOnlyList<PositionDto>>(HttpMethod.Get, url, null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<ApiResponse<PositionDto>?> CreatePositionAsync(CreatePositionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<PositionDto>(HttpMethod.Post, "api/v1/employees/positions", request, cancellationToken);
    }

    public async Task<ApiResponse<PositionDto>?> UpdatePositionAsync(long id, UpdatePositionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<PositionDto>(HttpMethod.Put, $"api/v1/employees/positions/{id}", request, cancellationToken);
    }

    public async Task<IReadOnlyList<SalaryProfileDto>> GetSalaryHistoryAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<SalaryProfileDto>>(HttpMethod.Get, $"api/v1/employees/{employeeId}/salary-history", null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<IReadOnlyList<EmployeeTimelineEventDto>> GetEmployeeTimelineAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<EmployeeTimelineEventDto>>(HttpMethod.Get, $"api/v1/employees/{employeeId}/timeline", null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<string>>(HttpMethod.Get, "api/v1/employees/roles", null, cancellationToken);
        return response?.Data ?? [];
    }

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

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }
}
