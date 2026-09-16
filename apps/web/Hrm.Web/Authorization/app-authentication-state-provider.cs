using System.Globalization;
using System.Security.Claims;
using Hrm.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace Hrm.Web.Authorization;

public sealed class AppAuthenticationStateProvider(BrowserSessionStore sessionStore) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await sessionStore.GetAsync();
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
