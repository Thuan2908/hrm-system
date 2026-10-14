using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using Hrm.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.Web.Authorization;

public sealed class AppAuthenticationStateProvider(
    BrowserSessionStore sessionStore,
    IServiceProvider serviceProvider) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await sessionStore.GetAsync();
        if (session is not null &&
            session.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(30) &&
            !string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            try
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                using var response = await httpClient.PostAsJsonAsync(
                    "api/v1/auth/refresh",
                    new RefreshTokenRequest(session.RefreshToken));
                if (response.IsSuccessStatusCode)
                {
                    var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>();
                    if (envelope?.Data is not null)
                    {
                        await sessionStore.SaveAsync(envelope.Data);
                        return CreateState(envelope.Data);
                    }
                }

                await sessionStore.ClearAsync();
                return CreateState(null);
            }
            catch
            {
                await sessionStore.ClearAsync();
                return CreateState(null);
            }
        }

        return CreateState(session);
    }

    public void SetAuthenticated(AuthTokenResponse session) =>
        NotifyAuthenticationStateChanged(Task.FromResult(CreateState(session)));

    public void SetAnonymous() =>
        NotifyAuthenticationStateChanged(Task.FromResult(CreateState(null)));

    private static AuthenticationState CreateState(AuthTokenResponse? session)
    {
        if (session is null || session.AccessTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, session.User.UserName),
            new("full_name", session.User.FullName)
        };
        claims.AddRange(session.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(session.User.Permissions.Select(permission => new Claim("permission", permission)));
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")));
    }
}
