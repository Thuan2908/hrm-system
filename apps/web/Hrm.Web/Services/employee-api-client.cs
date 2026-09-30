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

    Task<bool> OffboardEmployeeAsync(long employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default);
}

public sealed class EmployeeApiClient(HttpClient httpClient, IAuthApiClient authApiClient) : IEmployeeApiClient
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

    public async Task<bool> OffboardEmployeeAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<object>(HttpMethod.Post, $"api/v1/employees/{employeeId}/offboard", null, cancellationToken);
        if (response is null || !response.Success)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Không thể thực hiện thôi việc cho nhân sự.");
        }
        return true;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<DepartmentDto>>(HttpMethod.Get, "api/v1/employees/departments", null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<PositionDto>>(HttpMethod.Get, "api/v1/employees/positions", null, cancellationToken);
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

        var token = await authApiClient.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }
}
