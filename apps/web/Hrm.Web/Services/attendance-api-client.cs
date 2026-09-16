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

public sealed class AttendanceApiClient(HttpClient httpClient) : IAttendanceApiClient
{
    public async Task<AttendanceTodayResponse?> GetTodayStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<ApiResponse<AttendanceTodayResponse>>(
            "api/v1/attendance/today", cancellationToken);
        return response?.Data;
    }

    public async Task<CheckInResultDto?> CheckInAsync(string? notes = null, CancellationToken cancellationToken = default)
    {
        using var httpResponse = await httpClient.PostAsJsonAsync(
            "api/v1/attendance/check-in", new CheckInRequest(notes), cancellationToken);

        var envelope = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<CheckInResultDto>>(cancellationToken: cancellationToken);
        if (!httpResponse.IsSuccessStatusCode || envelope is null || !envelope.Success || envelope.Data is null)
        {
            throw new InvalidOperationException(envelope?.Error?.Message ?? "Lỗi khi điểm danh vào.");
        }

        return envelope.Data;
    }

    public async Task<CheckOutResultDto?> CheckOutAsync(string? notes = null, CancellationToken cancellationToken = default)
    {
        using var httpResponse = await httpClient.PostAsJsonAsync(
            "api/v1/attendance/check-out", new CheckOutRequest(notes), cancellationToken);

        var envelope = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<CheckOutResultDto>>(cancellationToken: cancellationToken);
        if (!httpResponse.IsSuccessStatusCode || envelope is null || !envelope.Success || envelope.Data is null)
        {
            throw new InvalidOperationException(envelope?.Error?.Message ?? "Lỗi khi điểm danh ra.");
        }

        return envelope.Data;
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetHistoryAsync(int days = 14, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<AttendanceRecordDto>>>(
            $"api/v1/attendance/history?days={days}", cancellationToken);
        return response?.Data ?? [];
    }
}
