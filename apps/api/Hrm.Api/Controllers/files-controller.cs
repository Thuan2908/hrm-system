using System.Security.Claims;
using Hrm.Contracts;
using Hrm.Modules.Files.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrm.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public sealed class FilesController(IFileService fileService) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<UploadFileResponse>>> Upload(
        IFormFile file,
        [FromForm] string moduleName = "General",
        [FromForm] string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail<UploadFileResponse>(
                new ApiError("INVALID_FILE", "Vui lòng chọn tệp tin cần tải lên.", [])));
        }

        var userId = GetUserId();
        await using var stream = file.OpenReadStream();

        var result = await fileService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            moduleName,
            referenceId,
            userId,
            cancellationToken);

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await fileService.GetFileDownloadAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse.Fail<string>(
                new ApiError("FILE_NOT_FOUND", "Không tìm thấy tệp tin hoặc tệp đã bị xóa.", [])));
        }

        return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StoredFileDto>>> GetMetadata(Guid id, CancellationToken cancellationToken)
    {
        var result = await fileService.GetMetadataAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse.Fail<StoredFileDto>(
                new ApiError("FILE_NOT_FOUND", "Không tìm thấy thông tin tệp tin.", [])));
        }

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("by-ref")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StoredFileDto>>>> GetByReference(
        [FromQuery] string moduleName,
        [FromQuery] string referenceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(moduleName) || string.IsNullOrWhiteSpace(referenceId))
        {
            return BadRequest(ApiResponse.Fail<IReadOnlyList<StoredFileDto>>(
                new ApiError("INVALID_ARGUMENT", "Vui lòng cung cấp phân hệ (moduleName) và mã tham chiếu (referenceId).", [])));
        }

        var result = await fileService.GetByReferenceAsync(moduleName, referenceId, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("ADMIN");

        var success = await fileService.DeleteAsync(id, userId, isAdmin, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponse.Fail<string>(
                new ApiError("FILE_NOT_FOUND", "Không tìm thấy tệp tin cần xóa.", [])));
        }

        return Ok(ApiResponse.Ok("Đã xóa tệp tin thành công."));
    }

    private long GetUserId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier is missing in security context.");
}
