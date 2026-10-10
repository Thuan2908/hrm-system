using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface IPayrollApiClient
{
    Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(CancellationToken cancellationToken = default);
    Task<PayslipDetailDto?> GetPayslipDetailAsync(long payslipId, CancellationToken cancellationToken = default);
    Task<YearlyPayrollSummaryDto?> GetMyYearlyPayslipsAsync(short year, CancellationToken cancellationToken = default);
}

public interface IPayrollManagementApiClient
{
    Task<IReadOnlyList<PayrollManagementItemDto>> GetAllPayslipsAsync(short? month = null, short? year = null, CancellationToken cancellationToken = default);
    Task<int> CalculatePayrollAsync(short month, short year, CancellationToken cancellationToken = default);
}

public sealed class PayrollApiClient(HttpClient httpClient, IAuthApiClient authApiClient) : IPayrollApiClient, IPayrollManagementApiClient
{
    public async Task<YearlyPayrollSummaryDto?> GetMyYearlyPayslipsAsync(short year, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<YearlyPayrollSummaryDto>(
            HttpMethod.Get, $"api/v1/payroll/my-payslips/yearly?year={year}", null, cancellationToken);
        return response?.Data;
    }

    public async Task<IReadOnlyList<PayrollManagementItemDto>> GetAllPayslipsAsync(short? month = null, short? year = null, CancellationToken cancellationToken = default)
    {
        var url = "api/v1/payroll-management/all";
        if (month.HasValue && year.HasValue) url += $"?month={month}&year={year}";
        var response = await SendAsync<IReadOnlyList<PayrollManagementItemDto>>(
            HttpMethod.Get, url, null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<int> CalculatePayrollAsync(short month, short year, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<int>(
            HttpMethod.Post, "api/v1/payroll-management/calculate", new CalculatePayrollRequest(month, year), cancellationToken);
        return response?.Data ?? 0;
    }
    public async Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<PayslipSummaryDto>>(
            HttpMethod.Get, "api/v1/payroll/my-payslips", null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<PayslipDetailDto?> GetPayslipDetailAsync(long payslipId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<PayslipDetailDto>(
            HttpMethod.Get, $"api/v1/payroll/my-payslips/{payslipId}", null, cancellationToken);
        return response?.Data;
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
