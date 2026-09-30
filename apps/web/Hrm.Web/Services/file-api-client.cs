using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hrm.Contracts;
using Microsoft.AspNetCore.Components.Forms;

namespace Hrm.Web.Services;

public interface IFileApiClient
{
    Task<UploadFileResponse?> UploadFileAsync(
        IBrowserFile file,
        string moduleName,
        string? referenceId = null,
        CancellationToken cancellationToken = default);

    Task<UploadFileResponse?> UploadStreamAsync(
        Stream stream,
        string fileName,
        string contentType,
        long sizeBytes,
        string moduleName,
        string? referenceId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredFileDto>> GetFilesByReferenceAsync(
        string moduleName,
        string referenceId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);
}

public sealed class FileApiClient(HttpClient httpClient, IAuthApiClient authApiClient) : IFileApiClient
{
    private const long MaxAllowedUploadSize = 15 * 1024 * 1024; // 15MB

    public async Task<UploadFileResponse?> UploadFileAsync(
        IBrowserFile file,
        string moduleName,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(MaxAllowedUploadSize, cancellationToken);
        return await UploadStreamAsync(
            stream,
            file.Name,
            file.ContentType,
            file.Size,
            moduleName,
            referenceId,
            cancellationToken);
    }

    public async Task<UploadFileResponse?> UploadStreamAsync(
        Stream stream,
        string fileName,
        string contentType,
        long sizeBytes,
        string moduleName,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(moduleName), "moduleName");

        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            content.Add(new StringContent(referenceId), "referenceId");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/files/upload")
        {
            Content = content
        };

        var token = await authApiClient.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UploadFileResponse>>(cancellationToken);

        if (apiResponse is null || !apiResponse.Success || apiResponse.Data is null)
        {
            throw new InvalidOperationException(apiResponse?.Error?.Message ?? "Không thể tải tệp tin lên hệ thống.");
        }

        return apiResponse.Data;
    }

    public async Task<IReadOnlyList<StoredFileDto>> GetFilesByReferenceAsync(
        string moduleName,
        string referenceId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/files/by-ref?moduleName={Uri.EscapeDataString(moduleName)}&referenceId={Uri.EscapeDataString(referenceId)}");

        var token = await authApiClient.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StoredFileDto>>>(cancellationToken);

        return apiResponse?.Data ?? [];
    }

    public async Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/files/{fileId}");

        var token = await authApiClient.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);

        if (apiResponse is null || !apiResponse.Success)
        {
            throw new InvalidOperationException(apiResponse?.Error?.Message ?? "Không thể xóa tệp tin.");
        }

        return true;
    }
}
