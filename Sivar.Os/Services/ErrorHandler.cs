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
        var requestId = Guid.NewGuid();
        var errorId = requestId.ToString();
        var startTime = DateTime.UtcNow;
        var logContext = string.IsNullOrWhiteSpace(context) ? exception.Message : $"{context}: {exception.Message}";

        _logger.LogInformation("[ErrorHandler.HandleError] START - RequestId={RequestId}, Timestamp={Timestamp}, Context={Context}, ExceptionType={ExceptionType}",
            requestId, startTime, context, exception?.GetType().Name ?? "null");

        try
        {
            if (exception == null)
            {
                _logger.LogError("[ErrorHandler.HandleError] EXCEPTION NULL - RequestId={RequestId}",
                    requestId);
                throw new ArgumentNullException(nameof(exception));
            }

            _logger.LogError(exception, "[ErrorHandler.HandleError] Handling error - RequestId={RequestId}, ErrorId={ErrorId}, Context={Context}, ExceptionType={ExceptionType}",
                requestId, errorId, logContext, exception.GetType().Name);

            var response = new ErrorResponse
            {
                ErrorId = errorId,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ArgumentNullException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    response.ErrorType = "ValidationError";
                    _logger.LogWarning("[ErrorHandler.HandleError] ArgumentNullException - RequestId={RequestId}, Message={Message}",
                        requestId, exception.Message);
                    break;

                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    response.ErrorType = "ValidationError";
                    _logger.LogWarning("[ErrorHandler.HandleError] ArgumentException - RequestId={RequestId}, Message={Message}",
                        requestId, exception.Message);
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    response.ErrorType = "AuthorizationError";
                    _logger.LogWarning("[ErrorHandler.HandleError] UnauthorizedAccessException - RequestId={RequestId}",
                        requestId);
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    response.ErrorType = "NotFoundError";
                    _logger.LogWarning("[ErrorHandler.HandleError] KeyNotFoundException - RequestId={RequestId}",
                        requestId);
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Message = exception.Message;
                    response.ErrorType = "ConflictError";
                    _logger.LogWarning("[ErrorHandler.HandleError] InvalidOperationException - RequestId={RequestId}, Message={Message}",
                        requestId, exception.Message);
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request timed out";
                    response.ErrorType = "TimeoutError";
                    response.IsRetryable = true;
                    _logger.LogWarning("[ErrorHandler.HandleError] TimeoutException (retryable) - RequestId={RequestId}",
                        requestId);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An internal server error occurred";
                    response.ErrorType = "InternalServerError";
                    response.IsRetryable = IsRetryable(exception);
                    _logger.LogError("[ErrorHandler.HandleError] InternalServerError - RequestId={RequestId}, IsRetryable={IsRetryable}",
                        requestId, response.IsRetryable);
                    break;
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ErrorHandler.HandleError] SUCCESS - RequestId={RequestId}, ErrorId={ErrorId}, StatusCode={StatusCode}, ErrorType={ErrorType}, Duration={Duration}ms",
                requestId, errorId, response.StatusCode, response.ErrorType, elapsed);

            return response;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ErrorHandler.HandleError] EXCEPTION in error handling - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, ex.GetType().Name, elapsed);
            throw;
        }
    }

    public void LogError(Exception exception, string context = "")
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var errorId = requestId.ToString();
        var logContext = string.IsNullOrWhiteSpace(context) ? exception.Message : $"{context}: {exception.Message}";

        _logger.LogInformation("[ErrorHandler.LogError] START - RequestId={RequestId}, Timestamp={Timestamp}, ErrorId={ErrorId}, Context={Context}",
            requestId, startTime, errorId, context);

        try
        {
            if (exception == null)
            {
                _logger.LogError("[ErrorHandler.LogError] EXCEPTION NULL - RequestId={RequestId}",
                    requestId);
                throw new ArgumentNullException(nameof(exception));
            }

            _logger.LogError(exception, "[ErrorHandler.LogError] Error {ErrorId}: {Context} - RequestId={RequestId}, ExceptionType={ExceptionType}",
                errorId, logContext, requestId, exception.GetType().Name);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ErrorHandler.LogError] SUCCESS - RequestId={RequestId}, ErrorId={ErrorId}, Duration={Duration}ms",
                requestId, errorId, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ErrorHandler.LogError] EXCEPTION in error logging - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, ex.GetType().Name, elapsed);
        }
    }

    public bool IsRetryable(Exception exception)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ErrorHandler.IsRetryable] START - RequestId={RequestId}, Timestamp={Timestamp}, ExceptionType={ExceptionType}",
            requestId, startTime, exception?.GetType().Name ?? "null");

        try
        {
            if (exception == null)
            {
                _logger.LogDebug("[ErrorHandler.IsRetryable] Exception is null - RequestId={RequestId}",
                    requestId);
                return false;
            }

            var isRetryable = exception is TimeoutException ||
                              exception is HttpRequestException ||
                              (exception is IOException);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ErrorHandler.IsRetryable] SUCCESS - RequestId={RequestId}, ExceptionType={ExceptionType}, IsRetryable={IsRetryable}, Duration={Duration}ms",
                requestId, exception.GetType().Name, isRetryable, elapsed);

            return isRetryable;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ErrorHandler.IsRetryable] EXCEPTION - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, ex.GetType().Name, elapsed);
            return false;
        }
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
