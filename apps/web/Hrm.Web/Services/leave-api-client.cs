using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface ILeaveApiClient
{
    Task<LeaveBalanceSummaryDto?> GetBalanceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default);
    Task<LeaveRequestDto?> CreateRequestAsync(CreateLeaveRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> CancelRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
}

public sealed class LeaveApiClient(HttpClient httpClient, IAuthApiClient authApiClient) : ILeaveApiClient
{
    public async Task<LeaveBalanceSummaryDto?> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<LeaveBalanceSummaryDto>(
            HttpMethod.Get, "api/v1/leave/balance", null, cancellationToken);
        return response?.Data;
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<LeaveRequestDto>>(
            HttpMethod.Get, "api/v1/leave/my-requests", null, cancellationToken);
        return response?.Data ?? [];
    }

    public async Task<LeaveRequestDto?> CreateRequestAsync(
        CreateLeaveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<LeaveRequestDto>(
            HttpMethod.Post, "api/v1/leave/request", request, cancellationToken);

        if (response is null || !response.Success || response.Data is null)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Không thể gửi đơn xin nghỉ phép.");
        }

        return response.Data;
    }

    public async Task<bool> CancelRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<string>(
            HttpMethod.Post, $"api/v1/leave/cancel/{requestId}", null, cancellationToken);

        if (response is null || !response.Success)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Không thể hủy đơn xin nghỉ phép.");
        }

        return true;
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
