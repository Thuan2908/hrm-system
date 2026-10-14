namespace Hrm.Modules.Files.Application;

public sealed class FileStorageOptions
{
    public string BasePath { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; } = 15728640; // 15MB
    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv",
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg",
        ".zip", ".rar", ".txt"
    ];
}
