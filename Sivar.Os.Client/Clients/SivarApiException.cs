using System.Net;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Exception thrown when API call fails
/// </summary>
public class SivarApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseContent { get; }

    public SivarApiException(HttpStatusCode statusCode, string message, string responseContent)
        : base($"API call failed with status {(int)statusCode} ({statusCode}): {message}")
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsBadRequest => StatusCode == HttpStatusCode.BadRequest;
    public bool IsServerError => (int)StatusCode >= 500;
}
