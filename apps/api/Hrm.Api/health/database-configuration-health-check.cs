using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hrm.Api.Health;

public sealed class DatabaseConfigurationHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var result = string.IsNullOrWhiteSpace(connectionString)
            ? HealthCheckResult.Unhealthy("Database connection is not configured.")
            : HealthCheckResult.Healthy("Database connection is configured.");

        return Task.FromResult(result);
    }
}
