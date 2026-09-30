using Hrm.Contracts;
using Hrm.Modules.Files.Domain;
using Hrm.Modules.Files.Infrastructure.Persistence;
using Hrm.Modules.Files.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Files.Application;

public interface IFileService
{
    Task<UploadFileResponse> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        long sizeBytes,
        string moduleName,
        string? referenceId,
        long userId,
        CancellationToken cancellationToken);

    Task<(Stream Stream, string ContentType, string FileName)?> GetFileDownloadAsync(
        Guid fileId,
        CancellationToken cancellationToken);

    Task<StoredFileDto?> GetMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredFileDto>> GetByReferenceAsync(
        string moduleName,
        string referenceId,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid fileId,
        long userId,
        bool isAdmin,
        CancellationToken cancellationToken);
}

public sealed class FileService(
    FilesDbContext dbContext,
    IFileStorageProvider storageProvider) : IFileService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv",
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg",
        ".zip", ".rar", ".txt"
    };

    private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15MB

    public async Task<UploadFileResponse> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        long sizeBytes,
        string moduleName,
        string? referenceId,
        long userId,
        CancellationToken cancellationToken)
    {
        if (sizeBytes <= 0 || stream.Length == 0)
        {
            throw new ArgumentException("Tệp tin không được rỗng.", nameof(stream));
        }

        if (sizeBytes > MaxFileSizeBytes)
        {
            throw new ArgumentException("Dung lượng tệp tin vượt quá giới hạn 15MB.", nameof(sizeBytes));
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Định dạng tệp tin '{extension}' không được hỗ trợ để đảm bảo an toàn hệ thống.", nameof(fileName));
        }

        var cleanFileName = Path.GetFileName(fileName);
        var storagePath = await storageProvider.SaveAsync(stream, cleanFileName, moduleName, cancellationToken);

        var fileAttachment = new FileAttachment
        {
            Id = Guid.NewGuid(),
            FileName = cleanFileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            FileSizeBytes = sizeBytes,
            StoragePath = storagePath,
            Module = string.IsNullOrWhiteSpace(moduleName) ? "General" : moduleName,
            ReferenceId = referenceId,
            UploadedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        dbContext.Files.Add(fileAttachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var downloadUrl = $"/api/v1/files/{fileAttachment.Id}/download";
        return new UploadFileResponse(
            fileAttachment.Id,
            fileAttachment.FileName,
            fileAttachment.ContentType,
            fileAttachment.FileSizeBytes,
            downloadUrl);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetFileDownloadAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var file = await dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

        if (file is null)
        {
            return null;
        }

        var stream = await storageProvider.OpenReadAsync(file.StoragePath, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        return (stream, file.ContentType, file.FileName);
    }

    public async Task<StoredFileDto?> GetMetadataAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var file = await dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

        if (file is null)
        {
            return null;
        }

        return MapToDto(file);
    }

    public async Task<IReadOnlyList<StoredFileDto>> GetByReferenceAsync(
        string moduleName,
        string referenceId,
        CancellationToken cancellationToken)
    {
        var list = await dbContext.Files
            .AsNoTracking()
            .Where(f => f.Module == moduleName && f.ReferenceId == referenceId && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteAsync(
        Guid fileId,
        long userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var file = await dbContext.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

        if (file is null)
        {
            return false;
        }

        if (!isAdmin && file.UploadedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa tệp tin này.");
        }

        file.IsDeleted = true;
        file.DeletedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await storageProvider.DeleteAsync(file.StoragePath, cancellationToken);

        return true;
    }

    private static StoredFileDto MapToDto(FileAttachment file) =>
        new(
            file.Id,
            file.FileName,
            file.ContentType,
            file.FileSizeBytes,
            file.Module,
            file.ReferenceId,
            file.UploadedByUserId,
            file.CreatedAt,
            $"/api/v1/files/{file.Id}/download");
}
