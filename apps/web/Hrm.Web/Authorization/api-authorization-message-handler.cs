using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Hrm.Contracts;

namespace Hrm.Web.Authorization;

public sealed class ApiAuthorizationMessageHandler(
    BrowserSessionStore sessionStore,
    AppAuthenticationStateProvider authenticationStateProvider) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAuthEndpoint = path.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase) ||
                             path.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase);

        if (!isAuthEndpoint)
        {
            var token = await GetValidAccessTokenAsync(request.RequestUri, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        byte[]? contentBytes = null;
        string? contentType = null;
        if (!isAuthEndpoint && request.Content != null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType?.ToString();
            request.Content = new ByteArrayContent(contentBytes);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthEndpoint)
        {
            var session = await sessionStore.GetAsync();
            if (!string.IsNullOrWhiteSpace(session?.RefreshToken))
            {
                var newToken = await RefreshTokenInternalAsync(request.RequestUri, session.RefreshToken, cancellationToken);
                if (!string.IsNullOrWhiteSpace(newToken))
                {
                    response.Dispose();

                    var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
                    if (contentBytes != null)
                    {
                        retryRequest.Content = new ByteArrayContent(contentBytes);
                        if (!string.IsNullOrWhiteSpace(contentType))
                        {
                            retryRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                        }
                    }

                    foreach (var header in request.Headers)
                    {
                        if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                        {
                            retryRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                    response = await base.SendAsync(retryRequest, cancellationToken);
                }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = response.Content == null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
            {
                var errorCode = response.StatusCode == HttpStatusCode.Unauthorized ? "UNAUTHORIZED" : "FORBIDDEN";
                var errorMessage = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "Phiên đăng nhập đã hết hạn."
                    : "Bạn không có quyền thực hiện thao tác này.";
                var fallbackEnvelope = ApiResponse.Fail<object>(new ApiError(errorCode, errorMessage, []));
                response.Content = JsonContent.Create(fallbackEnvelope);
            }
            else
            {
                var mediaType = response.Content?.Headers.ContentType?.MediaType ?? "application/json";
                response.Content = new StringContent(body, Encoding.UTF8, mediaType);
            }
        }

        return response;
    }

    private async Task<string?> GetValidAccessTokenAsync(Uri? requestUri, CancellationToken cancellationToken)
    {
        var session = await sessionStore.GetAsync();
        if (session is null) return null;

        if (session.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(30) &&
            !string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            return await RefreshTokenInternalAsync(requestUri, session.RefreshToken, cancellationToken);
        }

        return session.AccessToken;
    }

    private async Task<string?> RefreshTokenInternalAsync(Uri? requestUri, string refreshToken, CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            var currentSession = await sessionStore.GetAsync();
            if (currentSession != null &&
                currentSession.AccessTokenExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
            {
                return currentSession.AccessToken;
            }

            var baseUri = requestUri != null && requestUri.IsAbsoluteUri
                ? new Uri(requestUri.GetLeftPart(UriPartial.Authority))
                : null;
            var refreshUri = baseUri != null ? new Uri(baseUri, "api/v1/auth/refresh") : new Uri("api/v1/auth/refresh", UriKind.RelativeOrAbsolute);

            using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, refreshUri)
            {
                Content = JsonContent.Create(new RefreshTokenRequest(refreshToken))
            };

            using var response = await base.SendAsync(refreshRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await sessionStore.ClearAsync();
                authenticationStateProvider.SetAnonymous();
                return null;
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>(cancellationToken);
            if (envelope?.Data is null)
            {
                await sessionStore.ClearAsync();
                authenticationStateProvider.SetAnonymous();
                return null;
            }

            await sessionStore.SaveAsync(envelope.Data);
            authenticationStateProvider.SetAuthenticated(envelope.Data);
            return envelope.Data.AccessToken;
        }
        catch
        {
            await sessionStore.ClearAsync();
            authenticationStateProvider.SetAnonymous();
            return null;
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}
