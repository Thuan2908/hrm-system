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
