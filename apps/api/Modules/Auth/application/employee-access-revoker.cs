using System.Globalization;
using System.Text.Json;
using Hrm.Modules.Auth.Contracts;
using Hrm.Modules.Auth.Domain;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Auth.Application;

public sealed class EmployeeAccessRevoker(
    AuthDbContext dbContext,
    TimeProvider timeProvider) : IEmployeeAccessRevoker
{
    public async Task RevokeAsync(
        long employeeId,
        long actorUserId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(item => item.SecurityState)
            .SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (user is null)
        {
            return;
        }
        if (user.Id == actorUserId)
        {
            throw new Hrm.SharedKernel.DomainException(
                "OFFBOARD_SELF_FORBIDDEN",
                "Không thể tự thực hiện thôi việc cho chính tài khoản đang đăng nhập.");
        }

        var now = timeProvider.GetUtcNow();
        var before = new { user.IsActive, user.SecurityState?.LockoutEnd };
        user.IsActive = false;
        user.SecurityState ??= new UserSecurityState { UserId = user.Id };
        user.SecurityState.LockoutEnd = now.AddYears(10);
        user.SecurityState.FailedAccessCount = 0;

        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null && token.ExpiresAt > now)
            .ToArrayAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "employee.offboard.access-revoked",
            EntityType = "User",
            EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
            BeforeJson = JsonSerializer.Serialize(before),
            AfterJson = JsonSerializer.Serialize(new
            {
                user.IsActive,
                user.SecurityState.LockoutEnd,
                RevokedRefreshTokens = tokens.Length
            }),
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
