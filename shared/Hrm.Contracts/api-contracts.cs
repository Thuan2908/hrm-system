namespace Hrm.Contracts;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    ApiMeta? Meta,
    ApiError? Error);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, ApiMeta? meta = null) =>
        new(true, data, meta, null);

    public static ApiResponse<T> Fail<T>(ApiError error) =>
        new(false, default, null, error);
}

public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyCollection<ApiErrorDetail> Details);

public sealed record ApiErrorDetail(string? Field, string Message);

public sealed record ApiMeta(
    int? Page = null,
    int? PageSize = null,
    long? Total = null,
    int? TotalPages = null,
    string? TraceId = null);

public sealed record SystemInfoDto(
    string Name,
    string ApiVersion,
    DateTimeOffset UtcNow);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    long Total)
{
    public int TotalPages => Total == 0
        ? 0
        : (int)Math.Ceiling(Total / (double)PageSize);
}
