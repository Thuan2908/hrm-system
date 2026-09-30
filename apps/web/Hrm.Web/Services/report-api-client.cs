using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface IReportApiClient
{
    Task<HrDashboardSummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<HeadcountReportDto?> GetHeadcountReportAsync(CancellationToken cancellationToken = default);
    Task<TenureReportDto?> GetTenureReportAsync(CancellationToken cancellationToken = default);
    Task<AttendanceReportDto?> GetAttendanceReportAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<PayrollReportDto?> GetPayrollReportAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
}

public sealed class ReportApiClient(HttpClient httpClient, IAuthApiClient authApiClient) : IReportApiClient
{
    public async Task<HrDashboardSummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<HrDashboardSummaryDto>(
            HttpMethod.Get, "api/v1/reports/summary", cancellationToken);
        return response?.Data;
    }

    public async Task<HeadcountReportDto?> GetHeadcountReportAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<HeadcountReportDto>(
            HttpMethod.Get, "api/v1/reports/headcount", cancellationToken);
        return response?.Data;
    }

    public async Task<TenureReportDto?> GetTenureReportAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<TenureReportDto>(
            HttpMethod.Get, "api/v1/reports/tenure", cancellationToken);
        return response?.Data;
    }

    public async Task<AttendanceReportDto?> GetAttendanceReportAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var url = "api/v1/reports/attendance";
        if (month.HasValue && year.HasValue)
        {
            url += $"?month={month.Value}&year={year.Value}";
        }

        var response = await SendAsync<AttendanceReportDto>(
            HttpMethod.Get, url, cancellationToken);
        return response?.Data;
    }

    public async Task<PayrollReportDto?> GetPayrollReportAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var url = "api/v1/reports/payroll";
        if (month.HasValue && year.HasValue)
        {
            url += $"?month={month.Value}&year={year.Value}";
        }

        var response = await SendAsync<PayrollReportDto>(
            HttpMethod.Get, url, cancellationToken);
        return response?.Data;
    }

    private async Task<ApiResponse<T>?> SendAsync<T>(
        HttpMethod method,
        string uri,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);

        var token = await authApiClient.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }
}
