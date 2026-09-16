using Hrm.Contracts;
using Hrm.SharedKernel;
using Microsoft.AspNetCore.Diagnostics;

namespace Hrm.Api.ErrorHandling;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> LogUnhandledError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1000, nameof(LogUnhandledError)),
            "Unhandled request error. TraceId: {TraceId}");

    private static readonly Action<ILogger, string, string, Exception?> LogRejectedRequest =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1001, nameof(LogRejectedRequest)),
            "Request rejected with {ErrorCode}. TraceId: {TraceId}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, message) = exception switch
        {
            DomainException domainException =>
                (StatusCodes.Status422UnprocessableEntity, domainException.Code, domainException.Message),
            ArgumentException argumentException =>
                (StatusCodes.Status400BadRequest, "REQUEST_INVALID", argumentException.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, "UNEXPECTED_ERROR", "An unexpected error occurred.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledError(logger, httpContext.TraceIdentifier, exception);
        }
        else
        {
            LogRejectedRequest(logger, code, httpContext.TraceIdentifier, null);
        }

        var safeMessage = statusCode == StatusCodes.Status500InternalServerError && environment.IsProduction()
            ? "An unexpected error occurred."
            : message;

        httpContext.Response.StatusCode = statusCode;

        var response = ApiResponse.Fail<object>(
            new ApiError(code, safeMessage, Array.Empty<ApiErrorDetail>()));

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
