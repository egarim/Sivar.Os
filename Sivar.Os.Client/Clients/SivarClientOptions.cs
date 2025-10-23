namespace Sivar.Os.Client.Clients;

/// <summary>
/// Configuration options for SivarClient
/// </summary>
public class SivarClientOptions
{
    /// <summary>
    /// Base URL of the Sivar API (e.g., https://localhost:56010)
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:56010";

    /// <summary>
    /// Default timeout for HTTP requests
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable automatic retry on transient failures
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Authentication token (JWT)
    /// </summary>
    public string? AuthToken { get; set; }
}
