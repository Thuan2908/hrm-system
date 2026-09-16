using System.Text.Json;
using Hrm.Contracts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hrm.Api.Health;

public static class HealthResponseWriter
{
    public static HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate) =>
        new()
        {
            Predicate = predicate,
            ResponseWriter = WriteAsync
        };

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new
            {
                status = entry.Value.Status.ToString(),
                durationMilliseconds = entry.Value.Duration.TotalMilliseconds
            });

        var payload = ApiResponse.Ok<object>(
            new
            {
                status = report.Status.ToString(),
                totalDurationMilliseconds = report.TotalDuration.TotalMilliseconds,
                checks
            },
            new ApiMeta(TraceId: context.TraceIdentifier));

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
