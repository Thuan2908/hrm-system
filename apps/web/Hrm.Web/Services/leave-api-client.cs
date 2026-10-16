using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface ILeaveApiClient
{
    Task<LeaveBalanceSummaryDto?> GetBalanceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default);
    Task<LeaveRequestDto?> CreateRequestAsync(CreateLeaveRequestDto request, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto?> UpdateRequestAsync(Guid requestId, UpdateLeaveRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> CancelRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingLeaveApprovalDto>> GetPendingRequestsAsync(long? departmentId = null, string sortBy = "createdAt", bool descending = true, CancellationToken cancellationToken = default);
    Task<LeaveApprovalOptionsDto?> GetApprovalOptionsAsync(CancellationToken cancellationToken = default);
    Task<bool> ApproveRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> RejectRequestAsync(Guid requestId, string? reason, CancellationToken cancellationToken = default);
}

public sealed class LeaveApiClient(HttpClient httpClient) : ILeaveApiClient
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

    public async Task<LeaveApprovalOptionsDto?> GetApprovalOptionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<LeaveApprovalOptionsDto>(HttpMethod.Get, "api/v1/leave/approval-options", null, cancellationToken);
        if (response?.Success != true) throw new InvalidOperationException(response?.Error?.Message ?? "Không thể tải phạm vi duyệt đơn.");
        return response.Data;
    }

    public async Task<IReadOnlyList<PendingLeaveApprovalDto>> GetPendingRequestsAsync(
        long? departmentId = null, string sortBy = "createdAt", bool descending = true, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/leave/pending?sortBy={Uri.EscapeDataString(sortBy)}&descending={descending.ToString().ToLowerInvariant()}";
        if (departmentId.HasValue) url += $"&departmentId={departmentId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var response = await SendAsync<IReadOnlyList<PendingLeaveApprovalDto>>(
            HttpMethod.Get, url, null, cancellationToken);
        if (response?.Success != true) throw new InvalidOperationException(response?.Error?.Message ?? "Không thể tải đơn chờ duyệt.");
        return response?.Data ?? [];
    }

    public async Task<bool> ApproveRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<string>(
            HttpMethod.Post, $"api/v1/leave/{requestId}/approve", null, cancellationToken);

        if (response is null || !response.Success)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Không thể phê duyệt đơn nghỉ phép.");
        }

        return true;
    }

    public async Task<bool> RejectRequestAsync(Guid requestId, string? reason, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<string>(
            HttpMethod.Post, $"api/v1/leave/{requestId}/reject", new RejectLeaveRequestInput(reason), cancellationToken);

        if (response is null || !response.Success)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Không thể từ chối đơn nghỉ phép.");
        }

        return true;
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

    public async Task<LeaveRequestDto?> UpdateRequestAsync(
        Guid requestId,
        UpdateLeaveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<LeaveRequestDto>(
            HttpMethod.Put, $"api/v1/leave/requests/{requestId}", request, cancellationToken);

        if (response is null || !response.Success || response.Data is null)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Không thể cập nhật đơn xin nghỉ phép.");
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

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }
}
