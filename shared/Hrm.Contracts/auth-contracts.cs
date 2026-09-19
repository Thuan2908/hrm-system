namespace Hrm.Contracts;

public sealed record LoginRequest(string UserName, string Password, string? DeviceId = null);

public sealed record HeartbeatRequest(string? DeviceId = null);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    UserSessionDto User);

public sealed record UserSessionDto(
    long Id,
    string UserName,
    string FullName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record UpdateProfileRequest(string FullName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
