using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hrm.Contracts;
using Hrm.Modules.Auth.Domain;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hrm.Modules.Auth.Application;

public interface IAuthService
{
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<UserSessionDto> GetSessionAsync(long userId, CancellationToken cancellationToken);
}

public sealed class AuthService(
    AuthDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : IAuthService
{
    private readonly JwtOptions options = jwtOptions.Value;

    public async Task<AuthTokenResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(item => item.Employee)
            .Include(item => item.Role)
            .Include(item => item.SecurityState)
            .SingleOrDefaultAsync(item => EF.Functions.ILike(item.UserName, request.UserName.Trim()), cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (user?.SecurityState?.LockoutEnd > now)
        {
            throw new DomainException("AUTH_ACCOUNT_LOCKED", "Tài khoản đang bị khóa.");
        }

        var passwordValid = user is not null && VerifyPassword(request.Password, user.PasswordHash);
        if (user is null || !user.IsActive || !passwordValid)
        {
            if (user is not null)
            {
                user.SecurityState ??= new UserSecurityState { UserId = user.Id };
                user.SecurityState.FailedAccessCount++;
                if (user.SecurityState.FailedAccessCount >= 5)
                {
                    user.SecurityState.LockoutEnd = now.AddMinutes(15);
                    user.SecurityState.FailedAccessCount = 0;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            throw new DomainException("AUTH_INVALID_CREDENTIALS", "Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        if (user.SecurityState is not null)
        {
            user.SecurityState.FailedAccessCount = 0;
            user.SecurityState.LockoutEnd = null;
        }

        user.LastLoginAt = now.UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await IssueTokenPairAsync(user, ipAddress, now, cancellationToken);
    }

    public async Task<AuthTokenResponse> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User).ThenInclude(user => user.Employee)
            .Include(token => token.User).ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(token => token.TokenHash == HashToken(request.RefreshToken), cancellationToken);

        if (storedToken is null || !storedToken.IsActive(now) || !storedToken.User.IsActive)
        {
            throw new DomainException("AUTH_REFRESH_INVALID", "Phiên đăng nhập không còn hợp lệ.");
        }

        storedToken.RevokedAt = now;
        storedToken.RevokedByIp = ipAddress;
        var response = await IssueTokenPairAsync(storedToken.User, ipAddress, now, cancellationToken);
        storedToken.ReplacedByTokenHash = HashToken(response.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == HashToken(request.RefreshToken), cancellationToken);
        if (storedToken is null || storedToken.RevokedAt is not null) return;

        storedToken.RevokedAt = timeProvider.GetUtcNow();
        storedToken.RevokedByIp = ipAddress;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserSessionDto> GetSessionAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Include(item => item.Employee)
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new DomainException("AUTH_USER_NOT_FOUND", "Không tìm thấy tài khoản.");
        return await CreateSessionAsync(user, cancellationToken);
    }

    private async Task<AuthTokenResponse> IssueTokenPairAsync(
        ApplicationUser user,
        string? ipAddress,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        EnsureSigningKey();
        var session = await CreateSessionAsync(user, cancellationToken);
        var accessExpiresAt = now.AddMinutes(options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.UserName),
            new("full_name", user.Employee.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.Name.ToUpperInvariant())
        };
        claims.AddRange(session.Permissions.Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            options.Issuer, options.Audience, claims, now.UtcDateTime, accessExpiresAt.UtcDateTime, credentials);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(options.RefreshTokenDays),
            CreatedByIp = ipAddress
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(jwt), refreshToken, accessExpiresAt, session);
    }

    private async Task<UserSessionDto> CreateSessionAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var permissions = await (
            from grant in dbContext.RolePermissions.AsNoTracking()
            join permission in dbContext.Permissions.AsNoTracking() on grant.PermissionId equals permission.Id
            where grant.RoleId == user.RoleId
            select permission.Code)
            .Distinct().OrderBy(code => code).ToArrayAsync(cancellationToken);

        return new UserSessionDto(
            user.Id, user.UserName, user.Employee.FullName,
            [user.Role.Name.ToUpperInvariant()], permissions);
    }

    private void EnsureSigningKey()
    {
        if (options.SigningKey.Length < 32)
            throw new DomainException("AUTH_CONFIGURATION_INVALID", "JWT signing key phải có ít nhất 32 ký tự.");
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
