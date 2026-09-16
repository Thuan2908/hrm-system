using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface IAttendanceApiClient
{
    Task<AttendanceTodayResponse?> GetTodayStatusAsync(CancellationToken cancellationToken = default);
    Task<CheckInResultDto?> CheckInAsync(string? notes = null, CancellationToken cancellationToken = default);
    Task<CheckOutResultDto?> CheckOutAsync(string? notes = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecordDto>> GetHistoryAsync(int days = 14, CancellationToken cancellationToken = default);
}

public sealed class AttendanceApiClient(HttpClient httpClient, IAuthApiClient authApiClient) : IAttendanceApiClient
{
    public async Task<AttendanceTodayResponse?> GetTodayStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<AttendanceTodayResponse>(
            HttpMethod.Get, "api/v1/attendance/today", null, cancellationToken);
        return response?.Data;
    }

    public async Task<CheckInResultDto?> CheckInAsync(string? notes = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<CheckInResultDto>(
            HttpMethod.Post, "api/v1/attendance/check-in", new CheckInRequest(notes), cancellationToken);

        if (response is null || !response.Success || response.Data is null)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Lỗi khi điểm danh vào.");
        }

        return response.Data;
    }

    public async Task<CheckOutResultDto?> CheckOutAsync(string? notes = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<CheckOutResultDto>(
            HttpMethod.Post, "api/v1/attendance/check-out", new CheckOutRequest(notes), cancellationToken);

        if (response is null || !response.Success || response.Data is null)
        {
            throw new InvalidOperationException(response?.Error?.Message ?? "Lỗi khi điểm danh ra.");
        }

        return response.Data;
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetHistoryAsync(int days = 14, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<IReadOnlyList<AttendanceRecordDto>>(
            HttpMethod.Get, $"api/v1/attendance/history?days={days}", null, cancellationToken);
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
