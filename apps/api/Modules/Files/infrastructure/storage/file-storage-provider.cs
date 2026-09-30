using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Hrm.Modules.Files.Infrastructure.Storage;

public interface IFileStorageProvider
{
    Task<string> SaveAsync(Stream stream, string originalFileName, string moduleName, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}

public sealed class PhysicalFileStorageProvider : IFileStorageProvider
{
    private readonly string _baseStoragePath;

    public PhysicalFileStorageProvider(IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:BasePath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            _baseStoragePath = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
        }
        else
        {
            _baseStoragePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, configuredPath);
        }

        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<string> SaveAsync(Stream stream, string originalFileName, string moduleName, CancellationToken cancellationToken)
    {
        var safeModule = string.Join("_", moduleName.Split(Path.GetInvalidFileNameChars()));
        var safeFileName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeFileName);
        var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileNameWithoutExtension(safeFileName)}{extension}";

        var relativeFolder = Path.Combine(
            safeModule,
            DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture),
            DateTime.UtcNow.ToString("MM", CultureInfo.InvariantCulture));
        var targetFolder = Path.Combine(_baseStoragePath, relativeFolder);

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var fullFilePath = Path.Combine(targetFolder, uniqueFileName);
        await using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var relativeStoragePath = Path.Combine(relativeFolder, uniqueFileName).Replace('\\', '/');
        return relativeStoragePath;
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = GetValidatedFullPath(storagePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = GetValidatedFullPath(storagePath);
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (IOException)
            {
                // File might be locked temporarily
            }
        }

        return Task.CompletedTask;
    }

    private string GetValidatedFullPath(string storagePath)
    {
        var normalized = storagePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_baseStoragePath, normalized));

        if (!fullPath.StartsWith(_baseStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path: directory traversal attempt.");
        }

        return fullPath;
    }
}
