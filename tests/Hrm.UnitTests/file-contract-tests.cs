using Hrm.Contracts;

namespace Hrm.UnitTests;

public sealed class FileContractTests
{
    [Fact]
    public void UploadFileResponseInitializesWithCorrectProperties()
    {
        var id = Guid.NewGuid();
        var response = new UploadFileResponse(
            id,
            "document.pdf",
            "application/pdf",
            1024,
            $"/api/v1/files/{id}/download");

        Assert.Equal(id, response.Id);
        Assert.Equal("document.pdf", response.FileName);
        Assert.Equal("application/pdf", response.ContentType);
        Assert.Equal(1024, response.FileSizeBytes);
        Assert.Equal($"/api/v1/files/{id}/download", response.DownloadUrl);
    }

    [Fact]
    public void StoredFileDtoInitializesWithCorrectProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var dto = new StoredFileDto(
            id,
            "avatar.png",
            "image/png",
            2048,
            "Profile",
            "user-123",
            1001,
            now,
            $"/api/v1/files/{id}/download");

        Assert.Equal(id, dto.Id);
        Assert.Equal("avatar.png", dto.FileName);
        Assert.Equal("image/png", dto.ContentType);
        Assert.Equal(2048, dto.FileSizeBytes);
        Assert.Equal("Profile", dto.Module);
        Assert.Equal("user-123", dto.ReferenceId);
        Assert.Equal(1001, dto.UploadedByUserId);
        Assert.Equal(now, dto.CreatedAt);
        Assert.Equal($"/api/v1/files/{id}/download", dto.DownloadUrl);
    }
}
