namespace Hrm.Contracts;

public sealed record StoredFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Module,
    string? ReferenceId,
    long UploadedByUserId,
    DateTimeOffset CreatedAt,
    string DownloadUrl
);

public sealed record UploadFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DownloadUrl
);
