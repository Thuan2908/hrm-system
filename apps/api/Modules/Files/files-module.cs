using Hrm.Modules.Files.Application;
using Hrm.Modules.Files.Infrastructure.Persistence;
using Hrm.Modules.Files.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.Modules.Files;

public static class FilesModule
{
    public static IServiceCollection AddFilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<FilesDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IFilesDatabaseInitializer, FilesDatabaseInitializer>();
        services.AddSingleton<IFileStorageProvider, PhysicalFileStorageProvider>();
        services.AddScoped<IFileService, FileService>();

        return services;
    }
}
