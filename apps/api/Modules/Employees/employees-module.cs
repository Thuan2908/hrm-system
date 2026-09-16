using Hrm.Modules.Employees.Application;
using Hrm.Modules.Employees.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.Modules.Employees;

public static class EmployeesModule
{
    public static IServiceCollection AddEmployeesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<EmployeesDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IEmployeeOffboardingService, EmployeeOffboardingService>();
        return services;
    }
}
