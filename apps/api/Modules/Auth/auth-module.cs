using Hrm.Modules.Auth.Application;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.Modules.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<AuthDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IAuthDatabaseInitializer, AuthDatabaseInitializer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
