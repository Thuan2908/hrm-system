using Hrm.Modules.Reports.Application;
using Hrm.Modules.Reports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.Modules.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<ReportsDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
