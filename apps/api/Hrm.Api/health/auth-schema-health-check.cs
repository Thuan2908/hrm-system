using Hrm.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hrm.Api.Health;

public sealed class AuthSchemaHealthCheck(
    AuthDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await dbContext.Users.AsNoTracking().AnyAsync(cancellationToken);
            _ = await dbContext.Roles.AsNoTracking().AnyAsync(cancellationToken);
            _ = await dbContext.Permissions.AsNoTracking().AnyAsync(cancellationToken);
            _ = await dbContext.RolePermissions.AsNoTracking().AnyAsync(cancellationToken);
            _ = await dbContext.UserSecurityStates.AsNoTracking().AnyAsync(cancellationToken);
            _ = await dbContext.UserAccountMetadata.AsNoTracking().AnyAsync(cancellationToken);
            _ = await dbContext.RefreshTokens.AsNoTracking().AnyAsync(cancellationToken);
            _ = await dbContext.AuditLogs.AsNoTracking().AnyAsync(cancellationToken);
            return HealthCheckResult.Healthy("Security/Admin schema is compatible.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Security/Admin schema is missing or incompatible.",
                exception);
        }
    }
}
