using System.Net;

namespace Sivar.Os.Services;

/// <summary>
/// Service for handling and logging errors consistently
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Log an error and return a consistent error response
    /// </summary>
    ErrorResponse HandleError(Exception exception, string context = "");

    /// <summary>
    /// Log an error without creating a response
    /// </summary>
    void LogError(Exception exception, string context = "");

    /// <summary>
    /// Determine if an error should be retried
    /// </summary>
    bool IsRetryable(Exception exception);
}

public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;

    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        _logger = logger;
    }

    public ErrorResponse HandleError(Exception exception, string context = "")
    {
        var errorId = Guid.NewGuid().ToString();
        var logContext = string.IsNullOrWhiteSpace(context) ? exception.Message : $"{context}: {exception.Message}";

        _logger.LogError(exception, "Error {ErrorId}: {Context}", errorId, logContext);

        var response = new ErrorResponse
        {
            ErrorId = errorId,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                response.ErrorType = "ValidationError";
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                response.ErrorType = "AuthorizationError";
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Resource not found";
                response.ErrorType = "NotFoundError";
                break;

            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = exception.Message;
                response.ErrorType = "ConflictError";
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Message = "Request timed out";
                response.ErrorType = "TimeoutError";
                response.IsRetryable = true;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An internal server error occurred";
                response.ErrorType = "InternalServerError";
                response.IsRetryable = IsRetryable(exception);
                break;
        }

        return response;
    }

    public void LogError(Exception exception, string context = "")
    {
        var errorId = Guid.NewGuid().ToString();
        var logContext = string.IsNullOrWhiteSpace(context) ? exception.Message : $"{context}: {exception.Message}";
        _logger.LogError(exception, "Error {ErrorId}: {Context}", errorId, logContext);
    }

    public bool IsRetryable(Exception exception)
    {
        return exception is TimeoutException ||
               exception is HttpRequestException ||
               (exception is IOException);
    }
}

/// <summary>
/// Enhanced error response model
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorType { get; set; } = "Error";
    public string? ErrorId { get; set; }
    public string? Details { get; set; }
    public bool IsRetryable { get; set; } = false;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
