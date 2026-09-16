using System.Net.Http.Json;
using Hrm.Contracts;

namespace Hrm.Web.Services;

public interface ISystemApiClient
{
    Task<ApiResponse<SystemInfoDto>?> GetInfoAsync(CancellationToken cancellationToken = default);
}

public sealed class SystemApiClient(HttpClient httpClient) : ISystemApiClient
{
    public Task<ApiResponse<SystemInfoDto>?> GetInfoAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<ApiResponse<SystemInfoDto>>(
            "api/v1/system/info",
            cancellationToken);
}
