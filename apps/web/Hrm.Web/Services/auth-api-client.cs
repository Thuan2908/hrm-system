using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;
using Hrm.Web.Authorization;

namespace Hrm.Web.Services;

public interface IAuthApiClient
{
    Task<ApiError?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> RefreshAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task HeartbeatAsync(CancellationToken cancellationToken = default);
    Task<AccountStatusDto> CheckAccountStatusAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeProfileDto>?> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<ApiError?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ApiError?> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthApiClient(
    HttpClient httpClient,
    BrowserSessionStore sessionStore,
    AppAuthenticationStateProvider authenticationStateProvider) : IAuthApiClient, IDisposable
{
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    public async Task<ApiError?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var deviceId = !string.IsNullOrWhiteSpace(request.DeviceId)
            ? request.DeviceId
            : await sessionStore.GetOrCreateDeviceIdAsync();

        var payload = request with { DeviceId = deviceId };
        using var response = await httpClient.PostAsJsonAsync("api/v1/auth/login", payload, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>(cancellationToken);
        if (!response.IsSuccessStatusCode || envelope is null || !envelope.Success || envelope.Data is null)
        {
            return envelope?.Error ?? new ApiError("AUTH_REQUEST_FAILED", "Không thể đăng nhập.", []);
        }

        await sessionStore.SaveAsync(envelope.Data);
        authenticationStateProvider.SetAuthenticated(envelope.Data);
        return null;
    }

    public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            var current = await sessionStore.GetAsync();
            if (current is null)
            {
                return false;
            }

            using var response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/refresh",
                new RefreshTokenRequest(current.RefreshToken),
                cancellationToken);
            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>(cancellationToken);
            if (!response.IsSuccessStatusCode || envelope?.Data is null)
            {
                await ClearAsync();
                return false;
            }

            await sessionStore.SaveAsync(envelope.Data);
            authenticationStateProvider.SetAuthenticated(envelope.Data);
            return true;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var current = await sessionStore.GetAsync();
        try
        {
            if (current is not null)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout")
                {
                    Content = JsonContent.Create(new LogoutRequest(current.RefreshToken))
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.AccessToken);
                using var response = await httpClient.SendAsync(request, cancellationToken);
            }
        }
        finally
        {
            await ClearAsync();
        }
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var current = await sessionStore.GetAsync();
        if (current is null)
        {
            return null;
        }

        if (current.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return await RefreshAsync(cancellationToken)
                ? (await sessionStore.GetAsync())?.AccessToken
                : null;
        }

        return current.AccessToken;
    }

    public async Task HeartbeatAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(token)) return;

            var deviceId = await sessionStore.GetOrCreateDeviceIdAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/heartbeat")
            {
                Content = JsonContent.Create(new HeartbeatRequest(deviceId))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch
        {
            // Heartbeat failures should be silent
        }
    }

    public async Task<AccountStatusDto> CheckAccountStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AccountStatusDto(false, true, false, false, "unauthorized", "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/session-status");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new AccountStatusDto(false, true, false, false, "locked", "Phiên truy cập bị từ chối hoặc tài khoản đã bị khóa.");
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AccountStatusDto>>(cancellationToken);
            return envelope?.Data ?? new AccountStatusDto(false, true, false, false, "unknown", "Không nhận được phản hồi trạng thái hợp lệ.");
        }
        catch
        {
            // In case of transient network failure, don't immediately force logout unless verified
            return new AccountStatusDto(true, false, false, true, "active", "Đang hoạt động");
        }
    }

    public async Task<ApiResponse<EmployeeProfileDto>?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/profile");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeProfileDto>>(cancellationToken);
    }

    public async Task<ApiError?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ApiError("AUTH_UNAUTHORIZED", "Bạn chưa đăng nhập.", []);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, "api/v1/auth/profile")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<UserSessionDto>>(cancellationToken);

        if (!response.IsSuccessStatusCode || envelope is null || !envelope.Success || envelope.Data is null)
        {
            return envelope?.Error ?? new ApiError("PROFILE_UPDATE_FAILED", "Không thể cập nhật hồ sơ.", []);
        }

        // Cập nhật session và AuthenticationState để UI phản chiếu ngay lập tức
        var currentSession = await sessionStore.GetAsync();
        if (currentSession is not null)
        {
            var updatedSession = currentSession with { User = envelope.Data };
            await sessionStore.SaveAsync(updatedSession);
            authenticationStateProvider.SetAuthenticated(updatedSession);
        }

        return null;
    }

    public async Task<ApiError?> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ApiError("AUTH_UNAUTHORIZED", "Bạn chưa đăng nhập.", []);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/change-password")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken);

        if (!response.IsSuccessStatusCode || envelope is null || !envelope.Success)
        {
            return envelope?.Error ?? new ApiError("PASSWORD_CHANGE_FAILED", "Không thể đổi mật khẩu.", []);
        }

        return null;
    }

    private async Task ClearAsync()
    {
        await sessionStore.ClearAsync();
        authenticationStateProvider.SetAnonymous();
    }

    public void Dispose()
    {
        refreshLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
