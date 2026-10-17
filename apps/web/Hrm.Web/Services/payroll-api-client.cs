using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface IPayrollApiClient
{
    Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(short? year = null, long? employeeId = null, CancellationToken cancellationToken = default);
    Task<PayslipDetailDto?> GetPayslipDetailAsync(long payslipId, CancellationToken cancellationToken = default);
    Task<PayslipDetailDto?> GetPayslipByPeriodAsync(short month, short year, long? employeeId = null, CancellationToken cancellationToken = default);
    Task<YearlyPayrollSummaryDto?> GetMyYearlyPayslipsAsync(short year, CancellationToken cancellationToken = default);
}

public interface IPayrollManagementApiClient
{
    Task<IReadOnlyList<PayrollManagementItemDto>> GetAllPayslipsAsync(short? month = null, short? year = null, CancellationToken cancellationToken = default);
    Task<int> CalculatePayrollAsync(short month, short year, CancellationToken cancellationToken = default);
    Task<PayrollManagementItemDto?> AdjustPayrollRecordAsync(long payrollId, decimal actualDays, decimal bonusAmount, decimal deductions, CancellationToken cancellationToken = default);
}

public sealed class PayrollApiClient(HttpClient httpClient) : IPayrollApiClient, IPayrollManagementApiClient
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

    public async Task<PayrollManagementItemDto?> AdjustPayrollRecordAsync(long payrollId, decimal actualDays, decimal bonusAmount, decimal deductions, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<PayrollManagementItemDto>(
            HttpMethod.Post, "api/v1/payroll-management/adjust", new AdjustPayrollRecordRequest(payrollId, actualDays, bonusAmount, deductions), cancellationToken);
        return response?.Data;
    }

    public async Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(short? year = null, long? employeeId = null, CancellationToken cancellationToken = default)
    {
        var url = "api/v1/payroll/my-payslips";
        var queryParams = new List<string>();
        if (year.HasValue) queryParams.Add($"year={year.Value}");
        if (employeeId.HasValue) queryParams.Add($"employeeId={employeeId.Value}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        var response = await SendAsync<IReadOnlyList<PayslipSummaryDto>>(
            HttpMethod.Get, url, null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<PayslipDetailDto?> GetPayslipByPeriodAsync(short month, short year, long? employeeId = null, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/payroll/my-payslips/by-period?month={month}&year={year}";
        if (employeeId.HasValue) url += $"&employeeId={employeeId.Value}";

        var response = await SendAsync<PayslipDetailDto>(
            HttpMethod.Get, url, null, cancellationToken);
        return response?.Data;
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

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }
}
