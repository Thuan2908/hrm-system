using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface IPayrollApiClient
{
    Task<IReadOnlyList<PayslipSummaryDto>> GetMyPayslipsAsync(CancellationToken cancellationToken = default);
    Task<PayslipDetailDto?> GetPayslipDetailAsync(long payslipId, CancellationToken cancellationToken = default);
}

public sealed class PayrollApiClient(HttpClient httpClient, IAuthApiClient authApiClient) : IPayrollApiClient
{
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
