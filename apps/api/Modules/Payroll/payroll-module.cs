using Hrm.Modules.Payroll.Application;
using Hrm.Modules.Payroll.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.Modules.Payroll;

public static class PayrollModule
{
    public static IServiceCollection AddPayrollModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<PayrollDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IPayrollDatabaseInitializer, PayrollDatabaseInitializer>();
        services.AddScoped<IPayrollService, PayrollService>();

        return services;
    }
}
